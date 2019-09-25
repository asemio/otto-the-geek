using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public class OttoModel<TQuery>
        where TQuery : class
    {
        protected virtual SchemaBuilder<TQuery> ConfigureSchema(SchemaBuilder<TQuery> builder) => builder;

        public OttoServer CreateServer()
        {
            var services = new ServiceCollection();

            Schema schema = BuildGraphQLSchema(services);

            var provider = services.BuildServiceProvider();
            schema.DependencyResolver = provider.GetRequiredService<IDependencyResolver>();
            return new OttoServer(schema, provider);
        }

        public Schema BuildGraphQLSchema(ServiceCollection services)
        {
            var builder = ConfigureSchema(new SchemaBuilder<TQuery>());
            var ottoSchema = builder.Build(services);

            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<DataLoaderDocumentListener>();
            services.AddTransient(typeof(QueryFieldGraphqlResolverProxy<>));
            services.AddTransient<IDependencyResolver>(x => new FuncDependencyResolver(x.GetRequiredService));

            var schema = new Schema
            {
                Query = ottoSchema.QueryType
            };
            schema.RegisterTypes(ottoSchema.OtherTypes.ToArray());
            return schema;
        }
    }
}