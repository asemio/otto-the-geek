using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public abstract class OttoModel<TQuery> : OttoModel<TQuery, object, object>
        where TQuery : class {}
    public abstract class OttoModel<TQuery, TMutation> : OttoModel<TQuery, TMutation, object>
        where TQuery : class
        where TMutation : class {}

    public abstract class OttoModel<TQuery, TMutation, TSubscription>
        where TQuery : class
        where TMutation : class
        where TSubscription : class
    {
        protected virtual SchemaBuilder ConfigureSchema(SchemaBuilder builder) => builder;

        public OttoServer CreateServer()
        {
            var services = new ServiceCollection();

            Schema schema = BuildGraphQLSchema(services);

            var provider = services.BuildServiceProvider();
            schema.DependencyResolver = provider.GetRequiredService<IDependencyResolver>();
            return new OttoServer(schema, provider);
        }

        public ModelSchema<TQuery> BuildGraphQLSchema(IServiceCollection services)
        {
            var ottoSchema = BuildOttoSchema(services);

            return new ModelSchema<TQuery>(ottoSchema);
        }

        public OttoSchemaInfo BuildOttoSchema(IServiceCollection services)
        {
            var builder = ConfigureSchema(new SchemaBuilder(typeof(OttoTheGeek.Schema<TQuery, TMutation, TSubscription>)));
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