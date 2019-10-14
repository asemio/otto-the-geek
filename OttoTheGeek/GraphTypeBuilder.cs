using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
        where TModel : class
    {
        private readonly PropertyMap<FieldResolverConfiguration> _fieldResolvers;
        private readonly PropertyMap<Nullability> _nullabilityOverrides;
        private readonly PropertyMap<OrderByBuilder> _orderByBuilders;
        private readonly PropertyMap<Type> _graphTypeOverrides;
        private enum Nullability { Unspecified = 0, NonNull, Nullable }
        private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;
        private readonly IEnumerable<Type> _interfaces;
        private readonly string _customName;

        public GraphTypeBuilder() : this(
            new PropertyMap<FieldResolverConfiguration>(),
            new PropertyMap<Nullability>(),
            new PropertyMap<OrderByBuilder>(),
            new PropertyMap<Type>(),
            new PropertyInfo[0],
            new Type[0],
            null)
        {

        }
        private GraphTypeBuilder(
            PropertyMap<FieldResolverConfiguration> scalarFieldResolvers,
            PropertyMap<Nullability> nullabilityOverrides,
            PropertyMap<OrderByBuilder> orderByBuilders,
            PropertyMap<Type> graphTypeOverrides,
            IEnumerable<PropertyInfo> propertiesToIgnore,
            IEnumerable<Type> interfaces,
            string customName
            )
        {
            _fieldResolvers = scalarFieldResolvers;
            _propertiesToIgnore = propertiesToIgnore;
            _nullabilityOverrides = nullabilityOverrides;
            _orderByBuilders = orderByBuilders;
            _graphTypeOverrides = graphTypeOverrides;
            _interfaces = interfaces;
            _customName = customName;
        }

        public bool NeedsRegistration => _interfaces.Any();

        public ScalarFieldBuilder<TModel, TProp> ScalarField<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ScalarFieldBuilder<TModel, TProp>(this, prop);
        }

        public LooseScalarFieldBuilder<TModel, TProp> LooseScalarField<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new LooseScalarFieldBuilder<TModel, TProp>(this, prop);
        }

        public ListFieldBuilder<TModel, TProp> ListField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ListFieldBuilder<TModel, TProp>(this, prop);
        }

        public LooseListFieldBuilder<TModel, TProp> LooseListField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
        {
            return new LooseListFieldBuilder<TModel, TProp>(this, propertyExpression);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var props = _propertiesToIgnore.Concat(new[] { prop }).ToArray();

            return Clone(propertiesToIgnore: props);
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var dict = _nullabilityOverrides.Add(prop, Nullability.NonNull);

            return Clone(nullabilityOverrides: dict);
        }

        public GraphTypeBuilder<TModel> Nullable<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var dict = _nullabilityOverrides.Add(prop, Nullability.Nullable);

            return Clone(nullabilityOverrides: dict);
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity>(
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
            )
        {
            var prop = propSelector.PropertyInfoForSimpleGet();
            var map = _orderByBuilders.Add(prop, configurator(GetOrderByBuilder<TEntity>(prop)));

            return Clone(orderByBuilders: map);
        }

        public GraphTypeBuilder<TModel> Interface<TInterface>()
        {
            var tIface = typeof(TInterface);
            var tModel = typeof(TModel);

            if(!tIface.IsAssignableFrom(tModel))
            {
                throw new ArgumentException($"{tModel.Name} does not implement {tIface.Name}");
            }

            var interfaces = _interfaces.Concat(new[] {tIface}).Distinct().ToArray();

            return Clone(interfaces: interfaces);
        }

        public GraphTypeBuilder<TModel> Named(string name)
        {
            return Clone(customName: name);
        }

        internal GraphTypeBuilder<TModel> WithResolverConfiguration(PropertyInfo prop, FieldResolverConfiguration config)
        {
            var dict = _fieldResolvers.Add(prop, config);

            return Clone(fieldResolvers: dict);
        }

        internal GraphTypeBuilder<TModel> WithGraphTypeOverride(PropertyInfo prop, Type graphType)
        {
            var dict = _graphTypeOverrides.Add(prop, graphType);

            return Clone(graphTypeOverrides: dict);
        }

        IComplexGraphType IGraphTypeBuilder.BuildGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return BuildGraphType(cache, services);
        }
        public ComplexGraphType<TModel> BuildGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            var graphType = CreateGraphTypeCore(cache, services);
            graphType.Name = GraphTypeName;

            if(!cache.TryPrime(graphType))
            {
                return cache.GetOrCreate<TModel>(services);
            }

            foreach(var prop in typeof(TModel).GetProperties().Except(_propertiesToIgnore))
            {
                if(TryGetScalarGraphType(prop, out var graphQlType))
                {
                    graphType.Field(
                        type: graphQlType,
                        name: prop.Name
                    );
                }
                else
                {
                    var resolverConfig = _fieldResolvers.Get(prop);
                    if(resolverConfig == null)
                    {
                        throw new UnableToResolveException(prop);
                    }

                    graphType.AddField(fieldType: resolverConfig.ConfigureField(prop, cache, services));
                }
            }
            return graphType;
        }

        public QueryArguments BuildQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            var args = typeof(TModel)
                .GetProperties()
                .Except(_propertiesToIgnore)
                .Select(prop => ToQueryArgument(prop));

            return new QueryArguments(args);
        }

        private GraphTypeBuilder<TModel> Clone(
            PropertyMap<FieldResolverConfiguration> fieldResolvers = null,
            PropertyMap<Nullability> nullabilityOverrides = null,
            PropertyMap<OrderByBuilder> orderByBuilders = null,
            PropertyMap<Type> graphTypeOverrides = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null,
            IEnumerable<Type> interfaces = null,
            string customName = null
            )
        {
            return new GraphTypeBuilder<TModel>(
                scalarFieldResolvers: fieldResolvers ?? _fieldResolvers,
                nullabilityOverrides: nullabilityOverrides ?? _nullabilityOverrides,
                orderByBuilders: orderByBuilders ?? _orderByBuilders,
                propertiesToIgnore: propertiesToIgnore ?? _propertiesToIgnore,
                graphTypeOverrides: graphTypeOverrides ?? _graphTypeOverrides,
                interfaces: interfaces ?? _interfaces,
                customName: customName ?? _customName
            );
        }

        private string GraphTypeName =>
            _customName
            ??
            (
                IsConnection
                ? $"{GetConnectionElemType().Name}Connection"
                : typeof(TModel).Name
            );

        private bool IsConnection => typeof(TModel).IsConstructedGenericType
            && typeof(TModel).GetGenericTypeDefinition() == typeof(Connection<>);

        private Type GetConnectionElemType()
        {
            return typeof(TModel).GetGenericArguments().Single();
        }

        private QueryArgument ToQueryArgument(PropertyInfo prop)
        {
            if(TryGetScalarGraphType(prop, out var graphType))
            {
                return new QueryArgument(graphType)
                {
                    Name = prop.Name
                };
            }

            if(typeof(OrderValue).IsAssignableFrom(prop.PropertyType))
            {
                var builder = _orderByBuilders.Get(prop)
                    ?? OrderByBuilder.FromPropertyInfo(prop);

                var enumGraphType = builder.BuildGraphType();
                if(_nullabilityOverrides.Get(prop) == Nullability.NonNull)
                {
                    enumGraphType = new NonNullGraphType(enumGraphType);
                }
                return new QueryArgument(enumGraphType)
                {
                    Name = prop.Name
                };
            }

            throw new UnableToResolveException(prop);
        }

        private bool TryGetScalarGraphType(PropertyInfo prop, out Type type)
        {
            type = _graphTypeOverrides.Get(prop);

            if(type != null)
            {
                return true;
            }

            if(!ScalarTypeMap.TryGetGraphType(prop.PropertyType, out type))
            {
                if(!TryGetEnumType(prop, out type))
                {
                    return false;
                }
            }

            var nullability = _nullabilityOverrides.Get(prop);
            if(nullability == Nullability.Unspecified)
            {
                return true;
            }

            if(nullability == Nullability.NonNull)
            {
                type = type.MakeNonNullable();
            }
            else if(nullability == Nullability.Nullable)
            {
                type = type.UnwrapNonNullable();
            }
            return true;
        }

        private OrderByBuilder<TEntity> GetOrderByBuilder<TEntity>(PropertyInfo prop)
        {
            var builder = _orderByBuilders.Get(prop);

            return ((OrderByBuilder<TEntity>)builder) ?? new OrderByBuilder<TEntity>();
        }

        private ComplexGraphType<TModel> CreateGraphTypeCore(GraphTypeCache cache, IServiceCollection services)
        {
            if(typeof(TModel).IsInterface)
            {
                return new InterfaceGraphType<TModel>();
            }

            var objectGraphType = new ObjectGraphType<TModel>();
            foreach(var iFace in _interfaces)
            {
                objectGraphType.AddResolvedInterface((IInterfaceGraphType)cache.GetOrCreate(iFace, services));
            }

            return objectGraphType;
        }

        private bool TryGetEnumType(PropertyInfo prop, out Type type)
        {
            var propType = prop.PropertyType;
            type = null;
            if(propType.IsEnum)
            {
                type = typeof(NonNullGraphType<>).MakeGenericType(
                    typeof(OttoEnumGraphType<>).MakeGenericType(propType)
                );
                return true;
            }

            if(!propType.IsConstructedGenericType)
            {
                return false;
            }

            if(propType.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
                return false;
            }

            var innerType = propType.GetGenericArguments().Single();

            if(!innerType.IsEnum)
            {
                return false;
            }

            type = typeof(OttoEnumGraphType<>).MakeGenericType(innerType);
            return true;
        }
    }
}
