using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Core
{
    public class OttoModel<TQuery>
        where TQuery : class
    {
        protected virtual SchemaBuilder<TQuery> ConfigureSchema(SchemaBuilder<TQuery> builder) => builder;

        public OttoServer CreateServer()
        {
            var services = new ServiceCollection();
            var builder = ConfigureSchema(new SchemaBuilder<TQuery>());

            var ottoSchema = builder.Build(services);
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddTransient(typeof(QueryFieldGraphqlResolverProxy<>));

            var provider = services.BuildServiceProvider();
            var schema = new Schema {
                Query = ottoSchema.QueryType,
                DependencyResolver = new FuncDependencyResolver(t => provider.GetRequiredService(t))
            };

            return new OttoServer(schema, provider);
        }
    }
}