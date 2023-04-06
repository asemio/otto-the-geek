using System;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek
{
    public abstract class OttoModel
    {
        internal OttoModel() {}

        public abstract OttoSchemaConfig BuildConfig();
    }
    public abstract class OttoModel<TQuery> : OttoModel<TQuery, object, object> {}
    public abstract class OttoModel<TQuery, TMutation> : OttoModel<TQuery, TMutation, object> {}
    public abstract class OttoModel<TQuery, TMutation, TSubscription> : OttoModel
    {
        public OttoModel()
        {
        }
        protected virtual SchemaBuilder ConfigureSchema(SchemaBuilder builder) => builder;

        protected SchemaBuilder LoadConfigurators(Assembly assembly, SchemaBuilder builder)
        {
            var configuratorType = typeof(IModelConfigurator<>).MakeGenericType(GetType());
            var configurators = assembly.GetTypes()
                .Where(x => configuratorType.IsAssignableFrom(x))
                .Where(x => x.IsConcrete())
                .SelectMany(x => x.GetInterfaces(), (t, iface) => new { t, iface })
                .Where(x => x.iface.IsGenericFor(typeof(IGraphTypeConfigurator<,>)))
                .Select(x => ConfiguratorAdapter.Create(x.t, x.iface))
                .Cast<ConfiguratorAdapter>()
                .ToArray();

            return configurators.Aggregate(
                builder,
                (b, adapter) => adapter.Configure(b)
            );
        }

        public virtual OttoServer CreateServer(Action<IServiceCollection> configurator = null)
        {
            var services = new ServiceCollection();
            services
                .AddOtto(this);

            if(configurator != null)
            {
                configurator(services);
            }

            var provider = services.BuildServiceProvider();
            var schema = provider.GetRequiredService<ISchema>();

            return new OttoServer((Schema)schema, provider);
        }

        public override OttoSchemaConfig BuildConfig()
        {
            var builder = ConfigureSchema(new SchemaBuilder(typeof(Schema<TQuery, TMutation, TSubscription>)));

            return builder._schemaConfig;
        }
    }
}
