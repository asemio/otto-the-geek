using System;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public abstract class OttoModel
    {
        internal OttoModel() {}

        public abstract OttoSchemaInfo BuildOttoSchema(IServiceCollection services);
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
            Schema schema = new ModelSchema<OttoModel<TQuery, TMutation, TSubscription>>(BuildOttoSchema(services), provider);

            return new OttoServer(schema, provider);
        }

        public override OttoSchemaInfo BuildOttoSchema(IServiceCollection services)
        {
            var builder = ConfigureSchema(new SchemaBuilder(typeof(Schema<TQuery, TMutation, TSubscription>)));
            var ottoSchema = builder.Build(services);

            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<DataLoaderDocumentListener>();
            services.AddTransient(typeof(QueryFieldGraphqlResolverProxy<>));
            services.AddTransient<TimeSpanGraphType>();
            services.AddTransient(typeof(OttoEnumGraphType<>));
            services.AddTransient(typeof(NonNullGraphType<>));
            services.AddTransient(typeof(IntGraphType));

            return ottoSchema;
        }
    }
}
