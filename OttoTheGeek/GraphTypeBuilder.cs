using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;
using OttoTheGeek.Internal.Authorization;
using OttoTheGeek.Internal.ResolverConfiguration;
using OttoTheGeek.TypeModel;
using SchemaBuilderCallback = System.Func<OttoTheGeek.SchemaBuilder, OttoTheGeek.SchemaBuilder>;

namespace OttoTheGeek {
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
    where TModel : class {
        [Obsolete]
        internal readonly GraphTypeConfiguration<TModel> _config;

        private readonly IEnumerable<SchemaBuilderCallback> _schemaBuilderCallbacks;

        public GraphTypeBuilder (ScalarTypeMap scalarTypeMap) : this (
            new GraphTypeConfiguration<TModel> (scalarTypeMap),
            new SchemaBuilderCallback[0],
            OttoTypeConfig.ForOutputType<TModel>()
            ) {

        }
        private GraphTypeBuilder(
            GraphTypeConfiguration<TModel> config,
            IEnumerable<SchemaBuilderCallback> schemaBuilderCallbacks,
            OttoTypeConfig typeConfig
            ) {
            _config = config;
            _schemaBuilderCallbacks = schemaBuilderCallbacks;
            TypeConfig = typeConfig;
        }

        public bool NeedsRegistration => _config.NeedsRegistration;
        public OttoTypeConfig TypeConfig { get; }

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

            return new ListFieldBuilder<TModel, TProp> (this, prop, _config.ScalarTypeMap);
        }

        public ConnectionFieldBuilder<TModel, TProp> ConnectionField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
            where TProp : class
        {
            return new ConnectionFieldBuilder<TModel, TProp>(this, propertyExpression);
        }

        public LooseListFieldBuilder<TModel, TProp> LooseListField<TProp> (Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression) {
            return new LooseListFieldBuilder<TModel, TProp> (this, propertyExpression, _config.ScalarTypeMap);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp> (Expression<Func<TModel, TProp>> propertyExpression) {
            var prop = propertyExpression.PropertyInfoForSimpleGet ();
            var props = _config.PropsToIgnore.Concat (new [] { prop }).ToArray ();

            return Clone (
                _config.Clone (propertiesToIgnore: props),
                TypeConfig.IgnoreProperty(prop)
                );
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(
                _config.ConfigureField(propertyExpression, x => x.WithNullable(Nullability.NonNull)),
                TypeConfig.ConfigureField(propertyExpression, x => x with { Nullability = Nullability.NonNull })
                );
        }

        public GraphTypeBuilder<TModel> Nullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(
                _config.ConfigureField(propertyExpression, x => x.WithNullable(Nullability.Nullable)),
                TypeConfig.ConfigureField(propertyExpression, x => x with { Nullability = Nullability.Nullable })
                );
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity> (
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
        ) {
            return Clone (
                _config.ConfigureField(propSelector, f => f.ConfigureOrderBy(configurator)),
                TypeConfig.ConfigureField(propSelector, cfg => cfg.ConfigureOrderBy(configurator))
                );
        }

        public AuthorizationBuilder<TModel, TProp> Authorize<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            return new AuthorizationBuilder<TModel, TProp>(this, propertyExpression);
        }

        public GraphTypeBuilder<TModel> Interface<TInterface> () {
            var tIface = typeof (TInterface);
            var tModel = typeof (TModel);

            if (!tIface.IsAssignableFrom (tModel)) {
                throw new ArgumentException ($"{tModel.Name} does not implement {tIface.Name}");
            }

            var interfaces = _config.Interfaces.Concat (new [] { tIface }).Distinct ().ToArray ();

            return Clone(
                _config.Clone(interfaces: interfaces),
                TypeConfig with {Interfaces = TypeConfig.Interfaces.Add(typeof(TInterface))}
            );
        }

        public GraphTypeBuilder<TModel> Named (string name) {
            return Clone (_config.Clone (customName: name), TypeConfig with { Name = name });
        }

        internal GraphTypeBuilder<TModel> WithResolverConfiguration (PropertyInfo prop, FieldResolverConfiguration config)
        {
            return Clone(
                _config.ConfigureField(prop, x => x.WithResolverConfiguration(config)),
                TypeConfig.ConfigureField(prop, x => x with { ResolverConfiguration = config })
                );
        }

        internal GraphTypeBuilder<TModel> WithGraphTypeOverride (PropertyInfo prop, Type graphType)
        {
            return Clone(_config.ConfigureField(prop, x => x.OverrideGraphType(graphType)), TypeConfig);
        }

        internal GraphTypeBuilder<TModel> WithTypeConfig(Func<OttoTypeConfig, OttoTypeConfig> configurator)
        {
            return Clone(_config, configurator(TypeConfig));
        }

        IComplexGraphType IGraphTypeBuilder.BuildGraphType (GraphTypeCache cache, IServiceCollection services)
            => BuildGraphType (cache, services);

        public ComplexGraphType<TModel> BuildGraphType (GraphTypeCache cache, IServiceCollection services) {
            var graphType = CreateGraphTypeCore (cache, services);
            graphType.Name = GraphTypeName;
            graphType.Description = typeof(TModel).GetCustomAttribute<DescriptionAttribute>()?.Description;

            if (!cache.TryPrime (graphType)) {
                return cache.GetOrCreate<TModel> (services);
            }

            foreach (var prop in TypeConfig.GetRelevantProperties())
            {
                var fieldConfig = _config.GetFieldConfig(prop);
                fieldConfig.ConfigureField(graphType, cache, services);
            }

            if(graphType is ObjectGraphType<TModel> objectGraphType)
            {
                foreach (var iFace in _config.Interfaces) {
                    objectGraphType.AddResolvedInterface ((IInterfaceGraphType) cache.GetOrCreate (iFace, services));
                }
            }

            return graphType;
        }

        public QueryArguments BuildQueryArguments(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> inputTypesCache)
        {
            return TypeConfig.ToGqlNetArguments(config, inputTypesCache);
        }

        /// <summary>
        /// This is an escape hatch to allow you to configure other graph types
        /// </summary>
        public GraphTypeBuilder<TModel> WithSchemaBuilderCallback(SchemaBuilderCallback callback)
        {
            return new GraphTypeBuilder<TModel>(_config, _schemaBuilderCallbacks.Concat(new[] { callback }).ToArray(), TypeConfig);
        }

        internal (SchemaBuilder, GraphTypeBuilder<TModel>) RunSchemaBuilderCallbacks(SchemaBuilder builder)
        {
            if(!_schemaBuilderCallbacks.Any())
            {
                return (builder, this);
            }

            builder = _schemaBuilderCallbacks.Aggregate(builder, (b, func) => func(b));

            return (builder, new GraphTypeBuilder<TModel>(_config, new SchemaBuilderCallback[0], TypeConfig));
        }

        internal GraphTypeBuilder<TModel> Clone (GraphTypeConfiguration<TModel> config, OttoTypeConfig typeConfig) {
            return new GraphTypeBuilder<TModel> (config, _schemaBuilderCallbacks, typeConfig);
        }

        public string GraphTypeName =>
            _config.CustomName ??
            (
                IsConnection ?
                $"{GetConnectionElemType().Name}Connection" :
                SanitizedTypeName()
            );

        private bool IsConnection => typeof (TModel).IsConstructedGenericType &&
            typeof (TModel).GetGenericTypeDefinition () == typeof (Connection<>);

        private Type GetConnectionElemType () {
            return typeof (TModel).GetGenericArguments ().Single ();
        }

        private string SanitizedTypeName()
        {
            var modelType = typeof(TModel);
            var typeName = modelType.Name;

            if(!modelType.IsGenericType)
            {
                return typeName;
            }

            var closedType = modelType.GetGenericArguments()[0];

            var trimmedName = typeName.Substring(0, typeName.IndexOf('`'));

            return $"{trimmedName}Of{closedType.Name}";
        }

        private QueryArgument ToQueryArgument (PropertyInfo prop, GraphTypeCache cache) {
            var fieldConfig = _config.GetFieldConfig(prop);

            var desc = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;

            if (fieldConfig.TryGetScalarGraphType (out var graphType)) {
                return new QueryArgument (graphType) {
                    Name = prop.Name,
                    Description = desc
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
                    Name = prop.Name,
                    Description = desc,
                };
            }
            var elemType = prop.PropertyType.GetEnumerableElementType ();
            if (elemType != null)
            {
                if (_config.ScalarTypeMap.TryGetGraphType(elemType, out var scalarElemGraphType))
                {
                    var listGraphType = typeof(ListGraphType<>).MakeGenericType(scalarElemGraphType);

                    return new QueryArgument(listGraphType)
                    {
                        Name = prop.Name,
                        Description = desc,
                    };
                }

                var complexElemGraphType = cache.GetOrCreateInputType(elemType);
                var listType = new ListGraphType(new NonNullGraphType(complexElemGraphType));
                return new QueryArgument(listType)
                {
                    Name = prop.Name,
                    Description = desc,
                };
            }

            var inputType = cache.GetOrCreateInputType(prop.PropertyType);

            return new QueryArgument(fieldConfig.TryWrapNonNull(inputType))
            {
                Name = prop.Name,
                Description = desc,
            };
        }

        private ComplexGraphType<TModel> CreateGraphTypeCore (GraphTypeCache cache, IServiceCollection services) {
            if (typeof (TModel).IsInterface) {
                return new InterfaceGraphType<TModel> ();
            }

            var objectGraphType = new ObjectGraphType<TModel> ();
            return objectGraphType;
        }
    }
}
