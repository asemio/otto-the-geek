using System.Linq;
using GraphQL;
using GraphQL.Server;
using GraphQL.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOtto<TModel>(this IServiceCollection services, TModel model)
            where TModel : OttoModel
        {
            var ottoSchema = model.BuildOttoSchema(services);

            return services
                .AddTransient(ctx => {
                    var schema = new ModelSchema<TModel>(ottoSchema, ctx);

                    return schema;
                })
                .TryRegisterGraphQLServer();
        }

        private static IServiceCollection TryRegisterGraphQLServer(this IServiceCollection services)
        {
            // using IDocumentWriter to check for registration already present
            if (!services.Any(x => x.ServiceType == typeof(GraphQL.IDocumentWriter)))
            {
                services
                    .AddGraphQL()
                    .AddSystemTextJson()
                    .AddGraphTypes(ServiceLifetime.Scoped)
                    .AddDataLoader();
                services
                    .AddSingleton<IDocumentExecuter, DocumentExecuter>()
                    .AddSingleton<IDocumentWriter, DocumentWriter>();
            }

            return services;
        }
    }
}