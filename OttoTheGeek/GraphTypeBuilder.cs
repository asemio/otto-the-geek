using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OttoTheGeek.Internal;
using OttoTheGeek.Internal.Authorization;
using OttoTheGeek.Internal.ResolverConfiguration;
using OttoTheGeek.TypeModel;
using SchemaBuilderCallback = System.Func<OttoTheGeek.SchemaBuilder, OttoTheGeek.SchemaBuilder>;

namespace OttoTheGeek {
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
    where TModel : class {
        private readonly IEnumerable<SchemaBuilderCallback> _schemaBuilderCallbacks;

        public GraphTypeBuilder () : this (
            new SchemaBuilderCallback[0],
            OttoTypeConfig.ForOutputType<TModel>()
            ) {

        }
        private GraphTypeBuilder(
            IEnumerable<SchemaBuilderCallback> schemaBuilderCallbacks,
            OttoTypeConfig typeConfig
            ) {
            _schemaBuilderCallbacks = schemaBuilderCallbacks;
            TypeConfig = typeConfig;
        }

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

            return Clone (TypeConfig.IgnoreProperty(prop)
                );
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(TypeConfig.ConfigureField(propertyExpression, x => x with { Nullability = Nullability.NonNull })
                );
        }

        public GraphTypeBuilder<TModel> Nullable<TProp> (Expression<Func<TModel, TProp>> propertyExpression)
        {
            return Clone(
                TypeConfig.ConfigureField(propertyExpression, x => x with { Nullability = Nullability.Nullable })
                );
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity> (
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
        ) {
            return Clone (TypeConfig.ConfigureField(propSelector, cfg => cfg.ConfigureOrderBy(configurator))
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

            return Clone(TypeConfig with {Interfaces = TypeConfig.Interfaces.Add(typeof(TInterface))}
            );
        }

        public GraphTypeBuilder<TModel> Named (string name) {
            return Clone (TypeConfig with { Name = name });
        }

        internal GraphTypeBuilder<TModel> WithResolverConfiguration (PropertyInfo prop, FieldResolverConfiguration config)
        {
            return Clone(TypeConfig.ConfigureField(prop, x => x with { ResolverConfiguration = config })
                );
        }

        internal GraphTypeBuilder<TModel> WithTypeConfig(Func<OttoTypeConfig, OttoTypeConfig> configurator)
        {
            return Clone(configurator(TypeConfig));
        }

        /// <summary>
        /// This is an escape hatch to allow you to configure other graph types
        /// </summary>
        public GraphTypeBuilder<TModel> WithSchemaBuilderCallback(SchemaBuilderCallback callback)
        {
            return new GraphTypeBuilder<TModel>(_schemaBuilderCallbacks.Concat(new[] { callback }).ToArray(), TypeConfig);
        }

        internal (SchemaBuilder, GraphTypeBuilder<TModel>) RunSchemaBuilderCallbacks(SchemaBuilder builder)
        {
            if(!_schemaBuilderCallbacks.Any())
            {
                return (builder, this);
            }

            builder = _schemaBuilderCallbacks.Aggregate(builder, (b, func) => func(b));

            return (builder, new GraphTypeBuilder<TModel>(new SchemaBuilderCallback[0], TypeConfig));
        }

        internal GraphTypeBuilder<TModel> Clone (OttoTypeConfig typeConfig) {
            return new GraphTypeBuilder<TModel> (_schemaBuilderCallbacks, typeConfig);
        }
    }
}
