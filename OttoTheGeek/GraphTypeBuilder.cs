using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;
using SchemaBuilderCallback = System.Func<OttoTheGeek.SchemaBuilder, OttoTheGeek.SchemaBuilder>;

namespace OttoTheGeek {
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
    where TModel : class {
        private GraphTypeConfiguration<TModel> _config;

        private IEnumerable<SchemaBuilderCallback> _schemaBuilderCallbacks;

        public GraphTypeBuilder () : this (new GraphTypeConfiguration<TModel> (), new SchemaBuilderCallback[0]) {

        }
        private GraphTypeBuilder (GraphTypeConfiguration<TModel> config, IEnumerable<SchemaBuilderCallback> schemaBuilderCallbacks) {
            _config = config;
            _schemaBuilderCallbacks = schemaBuilderCallbacks;
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

        public ConnectionFieldBuilder<TModel, TProp> ConnectionField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
            where TProp : class
        {
            return new ConnectionFieldBuilder<TModel, TProp>(this, propertyExpression);
        }

        public LooseListFieldBuilder<TModel, TProp> LooseListField<TProp> (Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression) {
            return new LooseListFieldBuilder<TModel, TProp> (this, propertyExpression);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();
            var props = _config.PropsToIgnore.Concat (new [] { prop }).ToArray ();

            return Clone (_config.Clone (propertiesToIgnore: props));
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(_config.ConfigureField(propertyExpression, x => x.WithNullable(Nullability.NonNull)));
        }

        public GraphTypeBuilder<TModel> Nullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(_config.ConfigureField(propertyExpression, x => x.WithNullable(Nullability.Nullable)));
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity> (
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
        ) {
            return Clone (_config.ConfigureField(propSelector, f => f.ConfigureOrderBy(configurator)));
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

        internal GraphTypeBuilder<TModel> WithResolverConfiguration (PropertyInfo prop, FieldResolverConfiguration config)
        {
            return Clone(_config.ConfigureField(prop, x => x.WithResolverConfiguration(config)));
        }

        internal GraphTypeBuilder<TModel> WithGraphTypeOverride (PropertyInfo prop, Type graphType)
        {
            return Clone(_config.ConfigureField(prop, x => x.OverrideGraphType(graphType)));
        }

        IComplexGraphType IGraphTypeBuilder.BuildGraphType (GraphTypeCache cache, IServiceCollection services)
            => BuildGraphType (cache, services);

        public ComplexGraphType<TModel> BuildGraphType (GraphTypeCache cache, IServiceCollection services) {
            var graphType = CreateGraphTypeCore (cache, services);
            graphType.Name = GraphTypeName;

            if (!cache.TryPrime (graphType)) {
                return cache.GetOrCreate<TModel> (services);
            }

            foreach (var prop in typeof (TModel).GetProperties ().Except (_config.PropsToIgnore)) {
                var fieldConfig = _config.GetFieldConfig(prop);
                if (fieldConfig.TryGetScalarGraphType (out var graphQlType))
                {
                    graphType.Field (
                        type: graphQlType,
                        name: prop.Name
                    );
                }
                else
                {
                    if(fieldConfig.ResolverConfiguration == null)
                    {
                        throw new UnableToResolveException (prop);
                    }

                    graphType.AddField (fieldType: fieldConfig.ResolverConfiguration.ConfigureField (prop, cache, services));
                }

            }
            return graphType;
        }

        public InputObjectGraphType<TModel> BuildInputGraphType (GraphTypeCache cache) {
            var graphType = new InputObjectGraphType<TModel>
            {
                Name = GraphTypeName + "Input"
            };

            if (!cache.TryPrimeInput (graphType)) {
                return (InputObjectGraphType<TModel>)cache.GetOrCreateInputType(typeof(TModel));
            }

            foreach (var prop in typeof (TModel).GetProperties ().Except (_config.PropsToIgnore))
            {
                var fieldConfig = _config.GetFieldConfig(prop);
                fieldConfig.ConfigureInputTypeField(graphType, cache);
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

        internal GraphTypeBuilder<TModel> WithSchemaBuilderCallback(SchemaBuilderCallback callback)
        {
            return new GraphTypeBuilder<TModel>(_config, _schemaBuilderCallbacks.Concat(new[] { callback }).ToArray());
        }

        internal (SchemaBuilder, GraphTypeBuilder<TModel>) RunSchemaBuilderCallbacks(SchemaBuilder builder)
        {
            if(!_schemaBuilderCallbacks.Any())
            {
                return (builder, this);
            }

            builder = _schemaBuilderCallbacks.Aggregate(builder, (b, func) => func(b));

            return (builder, new GraphTypeBuilder<TModel>(_config, new SchemaBuilderCallback[0]));
        }

        private GraphTypeBuilder<TModel> Clone (GraphTypeConfiguration<TModel> config) {
            return new GraphTypeBuilder<TModel> (config, _schemaBuilderCallbacks);
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
            var fieldConfig = _config.GetFieldConfig(prop);

            if (fieldConfig.TryGetScalarGraphType (out var graphType)) {
                return new QueryArgument (graphType) {
                    Name = prop.Name
                };
            }

            if (typeof (OrderValue).IsAssignableFrom (prop.PropertyType)) {
                var builder = fieldConfig.OrderByBuilder ??
                    OrderByBuilder.FromPropertyInfo (prop);

                var enumGraphType = builder.BuildGraphType ();
                if (fieldConfig.Nullability == Nullability.NonNull) {
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

            var inputType = cache.GetOrCreateInputType(prop.PropertyType);

            return new QueryArgument(fieldConfig.TryWrapNonNull(inputType))
            {
                Name = prop.Name
            };
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
    }
}