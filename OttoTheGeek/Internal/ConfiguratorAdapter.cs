using System;

namespace OttoTheGeek.Internal
{
    internal sealed class ConfiguratorAdapter<TModel, TType> : ConfiguratorAdapter
        where TType : class
    {
        private readonly IGraphTypeConfigurator<TModel, TType> _configurator;

        public ConfiguratorAdapter(Type t)
        {
            _configurator = (IGraphTypeConfigurator<TModel, TType>)Activator.CreateInstance(t);
        }
        public override SchemaBuilder Configure(SchemaBuilder builder)
        {
            return builder.GraphType<TType>(_configurator.Configure);
        }
    }
    internal abstract class ConfiguratorAdapter
    {
        public abstract SchemaBuilder Configure(SchemaBuilder builder);

        public static ConfiguratorAdapter Create(Type t, Type ifaceType)
        {
            var adapterType = typeof(ConfiguratorAdapter<,>).MakeGenericType(ifaceType.GetGenericArguments());
            return (ConfiguratorAdapter)Activator.CreateInstance(adapterType, t);
        }
    }
}