using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;

namespace OttoTheGeek {
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
    where TModel : class {
        private GraphTypeConfiguration<TModel> _config;

        public GraphTypeBuilder () : this (new GraphTypeConfiguration<TModel> ()) {

        }
        private GraphTypeBuilder (GraphTypeConfiguration<TModel> config) {
            _config = config;
        }

        public bool NeedsRegistration => _config.NeedsRegistration;

        public ScalarFieldBuilder<TModel, TProp> ScalarField<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();

            return new ScalarFieldBuilder<TModel, TProp> (this, prop);
        }

        public LooseScalarFieldBuilder<TModel, TProp> LooseScalarField<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();

            return new LooseScalarFieldBuilder<TModel, TProp> (this, prop);
        }

        public ListFieldBuilder<TModel, TProp> ListField<TProp> (Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();

            return new ListFieldBuilder<TModel, TProp> (this, prop);
        }

        public LooseListFieldBuilder<TModel, TProp> LooseListField<TProp> (Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression) {
            return new LooseListFieldBuilder<TModel, TProp> (this, propertyExpression);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();
            var props = _config.PropsToIgnore.Concat (new [] { prop }).ToArray ();

            return Clone (_config.Clone (propertiesToIgnore: props));
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();
            var dict = _config.NullabilityOverrides.Add (prop, Nullability.NonNull);

            return Clone (_config.Clone (nullabilityOverrides: dict));
        }

        public GraphTypeBuilder<TModel> Nullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();
            var dict = _config.NullabilityOverrides.Add (prop, Nullability.Nullable);

            return Clone (_config.Clone (nullabilityOverrides: dict));
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity> (
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
        ) {
            var prop = propSelector.PropertyInfoForSimpleGet ();
            var map = _config.OrderByBuilders.Add (prop, configurator (GetOrderByBuilder<TEntity> (prop)));

            return Clone (_config.Clone (orderByBuilders: map));
        }

        public GraphTypeBuilder<TModel> Interface<TInterface> () {
            var tIface = typeof (TInterface);
            var tModel = typeof (TModel);

            if (!tIface.IsAssignableFrom (tModel)) {
                throw new ArgumentException ($"{tModel.Name} does not implement {tIface.Name}");
            }

            var interfaces = _config.Interfaces.Concat (new [] { tIface }).Distinct ().ToArray ();

            return Clone (_config.Clone (interfaces: interfaces));
        }

        public GraphTypeBuilder<TModel> Named (string name) {
            return Clone (_config.Clone (customName: name));
        }

        internal GraphTypeBuilder<TModel> WithResolverConfiguration (PropertyInfo prop, FieldResolverConfiguration config) {
            var dict = _config.FieldResolvers.Add (prop, config);

            return Clone (_config.Clone (fieldResolvers: dict));
        }

        internal GraphTypeBuilder<TModel> WithGraphTypeOverride (PropertyInfo prop, Type graphType) {
            var dict = _config.GraphTypeOverrides.Add (prop, graphType);

            return Clone (_config.Clone (graphTypeOverrides: dict));
        }

        IComplexGraphType IGraphTypeBuilder.BuildGraphType (GraphTypeCache cache, IServiceCollection services) {
            return BuildGraphType (cache, services);
        }
        public ComplexGraphType<TModel> BuildGraphType (GraphTypeCache cache, IServiceCollection services) {
            var graphType = CreateGraphTypeCore (cache, services);
            graphType.Name = GraphTypeName;

            if (!cache.TryPrime (graphType)) {
                return cache.GetOrCreate<TModel> (services);
            }

            foreach (var prop in typeof (TModel).GetProperties ().Except (_config.PropsToIgnore)) {
                if (TryGetScalarGraphType (prop, out var graphQlType)) {
                    graphType.Field (
                        type: graphQlType,
                        name: prop.Name
                    );
                } else {
                    var resolverConfig = _config.FieldResolvers.Get (prop);
                    if (resolverConfig == null) {
                        throw new UnableToResolveException (prop);
                    }

                    graphType.AddField (fieldType: resolverConfig.ConfigureField (prop, cache, services));
                }
            }
            return graphType;
        }

        public QueryArguments BuildQueryArguments (GraphTypeCache cache, IServiceCollection services) {
            var args = typeof (TModel)
                .GetProperties ()
                .Except (_config.PropsToIgnore)
                .Select (prop => ToQueryArgument (prop, cache));

            return new QueryArguments (args);
        }

        private GraphTypeBuilder<TModel> Clone (GraphTypeConfiguration<TModel> config) {
            return new GraphTypeBuilder<TModel> (config);
        }

        private string GraphTypeName =>
            _config.CustomName ??
            (
                IsConnection ?
                $"{GetConnectionElemType().Name}Connection" :
                typeof (TModel).Name
            );

        private bool IsConnection => typeof (TModel).IsConstructedGenericType &&
            typeof (TModel).GetGenericTypeDefinition () == typeof (Connection<>);

        private Type GetConnectionElemType () {
            return typeof (TModel).GetGenericArguments ().Single ();
        }

        private QueryArgument ToQueryArgument (PropertyInfo prop, GraphTypeCache cache) {
            if (TryGetScalarGraphType (prop, out var graphType)) {
                return new QueryArgument (graphType) {
                    Name = prop.Name
                };
            }

            if (typeof (OrderValue).IsAssignableFrom (prop.PropertyType)) {
                var builder = _config.OrderByBuilders.Get (prop) ??
                    OrderByBuilder.FromPropertyInfo (prop);

                var enumGraphType = builder.BuildGraphType ();
                if (_config.NullabilityOverrides.Get (prop) == Nullability.NonNull) {
                    enumGraphType = new NonNullGraphType (enumGraphType);
                }
                return new QueryArgument (enumGraphType) {
                    Name = prop.Name
                };
            }
            var elemType = prop.PropertyType.GetEnumerableElementType ();
            if (elemType != null && ScalarTypeMap.TryGetGraphType(elemType, out var elemGraphType))
            {
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(elemGraphType);

                return new QueryArgument(listGraphType)
                {
                    Name = prop.Name
                };
            }

            throw new UnableToResolveException (prop);
        }

        private bool TryGetScalarGraphType (PropertyInfo prop, out Type type)
        {
            type = _config.GraphTypeOverrides.Get(prop);

            if (type != null) {
                return true;
            }

            if (!ScalarTypeMap.TryGetGraphType (prop.PropertyType, out type))
            {
                if (!TryGetEnumType (prop, out type)) {
                    return false;
                }
            }

            var nullability = _config.NullabilityOverrides.Get (prop);
            if (nullability == Nullability.Unspecified) {
                return true;
            }

            if (nullability == Nullability.NonNull) {
                type = type.MakeNonNullable ();
            } else if (nullability == Nullability.Nullable) {
                type = type.UnwrapNonNullable ();
            }
            return true;
        }

        private OrderByBuilder<TEntity> GetOrderByBuilder<TEntity> (PropertyInfo prop) {
            var builder = _config.OrderByBuilders.Get (prop);

            return ((OrderByBuilder<TEntity>) builder) ?? new OrderByBuilder<TEntity> ();
        }

        private ComplexGraphType<TModel> CreateGraphTypeCore (GraphTypeCache cache, IServiceCollection services) {
            if (typeof (TModel).IsInterface) {
                return new InterfaceGraphType<TModel> ();
            }

            var objectGraphType = new ObjectGraphType<TModel> ();
            foreach (var iFace in _config.Interfaces) {
                objectGraphType.AddResolvedInterface ((IInterfaceGraphType) cache.GetOrCreate (iFace, services));
            }

            return objectGraphType;
        }

        private bool TryGetEnumType (PropertyInfo prop, out Type type) {
            var propType = prop.PropertyType;
            type = null;
            if (propType.IsEnum) {
                type = typeof (NonNullGraphType<>).MakeGenericType (
                    typeof (OttoEnumGraphType<>).MakeGenericType (propType)
                );
                return true;
            }

            if (!propType.IsConstructedGenericType) {
                return false;
            }

            if (propType.GetGenericTypeDefinition () != typeof (Nullable<>)) {
                return false;
            }

            var innerType = propType.GetGenericArguments ().Single ();

            if (!innerType.IsEnum) {
                return false;
            }

            type = typeof (OttoEnumGraphType<>).MakeGenericType (innerType);
            return true;
        }
    }
}