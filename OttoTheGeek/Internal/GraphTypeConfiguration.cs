using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OttoTheGeek.Internal
{
    internal sealed class GraphTypeConfiguration<T>
    {
        public PropertyMap<FieldResolverConfiguration> FieldResolvers { get; }
        public PropertyMap<Nullability> NullabilityOverrides { get; }
        public PropertyMap<OrderByBuilder> OrderByBuilders { get; }
        public PropertyMap<Type> GraphTypeOverrides { get; }
        public IEnumerable<PropertyInfo> PropsToIgnore { get; }
        public IEnumerable<Type> Interfaces { get; }
        public string CustomName { get; }

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
            FieldResolvers = scalarFieldResolvers;
            PropsToIgnore = propertiesToIgnore;
            NullabilityOverrides = nullabilityOverrides;
            OrderByBuilders = orderByBuilders;
            GraphTypeOverrides = graphTypeOverrides;
            Interfaces = interfaces;
            CustomName = customName;
        }
        public bool NeedsRegistration => Interfaces.Any();

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
                scalarFieldResolvers: fieldResolvers ?? FieldResolvers,
                nullabilityOverrides: nullabilityOverrides ?? NullabilityOverrides,
                orderByBuilders: orderByBuilders ?? OrderByBuilders,
                propertiesToIgnore: propertiesToIgnore ?? PropsToIgnore,
                graphTypeOverrides: graphTypeOverrides ?? GraphTypeOverrides,
                interfaces: interfaces ?? Interfaces,
                customName: customName ?? CustomName
            );
        }
    }
}