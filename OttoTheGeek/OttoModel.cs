using System;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{

    public abstract class OttoModel<TQuery> : OttoModel<TQuery, object, object> {}
    public abstract class OttoModel<TQuery, TMutation> : OttoModel<TQuery, TMutation, object> {}
    public abstract class OttoModel<TQuery, TMutation, TSubscription>
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

        public OttoServer CreateServer(Action<IServiceCollection> configurator = null)
        {
            var services = new ServiceCollection();
            if(configurator != null)
            {
                configurator(services);
            }

            Schema schema = BuildGraphQLSchema(services);

            var provider = services.BuildServiceProvider();
            schema.DependencyResolver = provider.GetRequiredService<IDependencyResolver>();
            return new OttoServer(schema, provider);
        }

        public ModelSchema BuildGraphQLSchema(IServiceCollection services)
        {
            var ottoSchema = BuildOttoSchema(services);

            return new ModelSchema(ottoSchema);
        }

        public OttoSchemaInfo BuildOttoSchema(IServiceCollection services)
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
            services.AddTransient<IDependencyResolver>(x => new FuncDependencyResolver(x.GetRequiredService));

            return ottoSchema;
        }
    }
}