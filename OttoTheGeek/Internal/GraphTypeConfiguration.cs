using System;
using System.Collections.Generic;
using System.Reflection;

namespace OttoTheGeek.Internal
{
    internal sealed class GraphTypeConfiguration<T>
    {
        private readonly PropertyMap<FieldResolverConfiguration> _fieldResolvers;
        private readonly PropertyMap<Nullability> _nullabilityOverrides;
        private readonly PropertyMap<OrderByBuilder> _orderByBuilders;
        private readonly PropertyMap<Type> _graphTypeOverrides;
        private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;
        private readonly IEnumerable<Type> _interfaces;
        private readonly string _customName;

        public GraphTypeConfiguration() : this(
            new PropertyMap<FieldResolverConfiguration>(),
            new PropertyMap<Nullability>(),
            new PropertyMap<OrderByBuilder>(),
            new PropertyMap<Type>(),
            new PropertyInfo[0],
            new Type[0],
            null)
        {

        }

        private GraphTypeConfiguration(
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

        public GraphTypeConfiguration<T> Clone(
            PropertyMap<FieldResolverConfiguration> fieldResolvers = null,
            PropertyMap<Nullability> nullabilityOverrides = null,
            PropertyMap<OrderByBuilder> orderByBuilders = null,
            PropertyMap<Type> graphTypeOverrides = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null,
            IEnumerable<Type> interfaces = null,
            string customName = null
            )
        {
            return new GraphTypeConfiguration<T>(
                scalarFieldResolvers: fieldResolvers ?? _fieldResolvers,
                nullabilityOverrides: nullabilityOverrides ?? _nullabilityOverrides,
                orderByBuilders: orderByBuilders ?? _orderByBuilders,
                propertiesToIgnore: propertiesToIgnore ?? _propertiesToIgnore,
                graphTypeOverrides: graphTypeOverrides ?? _graphTypeOverrides,
                interfaces: interfaces ?? _interfaces,
                customName: customName ?? _customName
            );
        }
    }
}