using System.Linq;
using GraphQL;
using GraphQL.Server;
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
                    var schema = new ModelSchema<TModel>(ottoSchema);

                    schema.DependencyResolver = new FuncDependencyResolver(t => ctx.GetRequiredService(t));

                    return schema;
                })
                .TryRegisterGraphQLServer();
        }

        private static IServiceCollection TryRegisterGraphQLServer(this IServiceCollection services)
        {
            // using IDocumentWriter to check for registration already present
            if (!services.Any(x => x.ServiceType == typeof(GraphQL.Http.IDocumentWriter)))
            {
                services
                    .AddGraphQL()
                    .AddGraphTypes(ServiceLifetime.Scoped)
                    .AddDataLoader();
            }

            return services;
        }
    }
}