using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek.Internal
{

    internal sealed class GraphTypeConfiguration<T>
    {
        private PropertyMap<FieldConfiguration<T>> _fieldConfig { get; }
        public IEnumerable<PropertyInfo> PropsToIgnore { get; }
        public IEnumerable<Type> Interfaces { get; }
        public string CustomName { get; }

        public GraphTypeConfiguration() : this(
            new PropertyInfo[0],
            new Type[0],
            new PropertyMap<FieldConfiguration<T>>(),
            null)
        {

        }

        private GraphTypeConfiguration(
            IEnumerable<PropertyInfo> propertiesToIgnore,
            IEnumerable<Type> interfaces,
            PropertyMap<FieldConfiguration<T>> fieldConfig,
            string customName
            )
        {
            PropsToIgnore = propertiesToIgnore;
            Interfaces = interfaces;
            _fieldConfig = fieldConfig;
            CustomName = customName;
        }
        public bool NeedsRegistration => Interfaces.Any();

        public GraphTypeConfiguration<T> ConfigureField<TProp>(Expression<Func<T, TProp>> propExpr, Func<FieldConfiguration<T>, FieldConfiguration<T>> configTransform)
        {
            var prop = propExpr.PropertyInfoForSimpleGet();

            return ConfigureField(prop, configTransform);
        }

        public GraphTypeConfiguration<T> ConfigureField(PropertyInfo prop, Func<FieldConfiguration<T>, FieldConfiguration<T>> configTransform)
        {
            var existingConfig = GetFieldConfig(prop);
            var newConfig = configTransform(existingConfig);

            return Clone(fieldConfig: _fieldConfig.Add(prop, newConfig));
        }

        public FieldConfiguration<T> GetFieldConfig(PropertyInfo prop)
        {
            return _fieldConfig.Get(prop)
                ?? new FieldConfiguration<T>(prop);
        }

        public GraphTypeConfiguration<T> Clone(
            PropertyMap<OrderByBuilder> orderByBuilders = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null,
            IEnumerable<Type> interfaces = null,
            PropertyMap<FieldConfiguration<T>> fieldConfig = null,
            string customName = null
            )
        {
            return new GraphTypeConfiguration<T>(
                propertiesToIgnore: propertiesToIgnore ?? PropsToIgnore,
                interfaces: interfaces ?? Interfaces,
                fieldConfig: fieldConfig ?? _fieldConfig,
                customName: customName ?? CustomName
            );
        }
    }
}