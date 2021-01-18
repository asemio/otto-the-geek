using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek.Internal
{

    internal sealed class GraphTypeConfiguration<T>
    {
        public ScalarTypeMap ScalarTypeMap { get; }
        private PropertyMap<FieldConfiguration<T>> _fieldConfig { get; }
        public IEnumerable<PropertyInfo> PropsToIgnore { get; }
        public IEnumerable<Type> Interfaces { get; }
        public string CustomName { get; }

        public GraphTypeConfiguration(ScalarTypeMap scalarTypeMap) : this(
            new PropertyInfo[0],
            new Type[0],
            new PropertyMap<FieldConfiguration<T>>(),
            null,
            scalarTypeMap)
        {

        }

        private GraphTypeConfiguration(
            IEnumerable<PropertyInfo> propertiesToIgnore,
            IEnumerable<Type> interfaces,
            PropertyMap<FieldConfiguration<T>> fieldConfig,
            string customName,
            ScalarTypeMap scalarTypeMap
            )
        {
            PropsToIgnore = propertiesToIgnore;
            Interfaces = interfaces;
            _fieldConfig = fieldConfig;
            CustomName = customName;
            ScalarTypeMap = scalarTypeMap;
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

        public bool IsPropertyIgnored(PropertyInfo prop)
        {
            return PropsToIgnore
                .Any(x => x.DeclaringType == prop.DeclaringType && x.Name == prop.Name);
        }

        public FieldConfiguration<T> GetFieldConfig(PropertyInfo prop)
        {
            return _fieldConfig.Get(prop)
                ?? new FieldConfiguration<T>(prop, ScalarTypeMap);
        }

        public GraphTypeConfiguration<T> Clone(
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
                customName: customName ?? CustomName,
                ScalarTypeMap
            );
        }
    }
}