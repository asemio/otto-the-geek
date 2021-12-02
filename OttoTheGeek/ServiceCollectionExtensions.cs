using System;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server;
using GraphQL.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOtto<TModel>(this IServiceCollection services, TModel model, Action<GraphQLOptions, IServiceProvider> configureOptions = null)
            where TModel : OttoModel
        {
            var ottoSchema = model.BuildOttoSchema(services);

            return services
                .AddTransient(ctx => {
                    var schema = new ModelSchema<TModel>(ottoSchema, ctx);

                    return schema;
                })
                .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
                .AddSingleton<DataLoaderDocumentListener>()
                .TryRegisterGraphQLServer(configureOptions);
        }

        private static IServiceCollection TryRegisterGraphQLServer(this IServiceCollection services, Action<GraphQLOptions, IServiceProvider> configureOptions)
        {
            if(configureOptions == null)
            {
                configureOptions = NopConfigureOptions;
            }
            // using IDocumentWriter to check for registration already present
            if (!services.Any(x => x.ServiceType == typeof(GraphQL.IDocumentWriter)))
            {
                services
                    .AddGraphQL((opts, sp) => {
                        opts.EnableMetrics = false;
                        configureOptions(opts, sp);
                    })
                    .AddSystemTextJson()
                    .AddGraphTypes(ServiceLifetime.Scoped)
                    .AddDataLoader();
                services
                    .AddSingleton<IDocumentExecuter, Internal.OttoDocumentExecuter>()
                    .AddSingleton<IDocumentWriter, DocumentWriter>();
            }

            return services;
        }

        private static void NopConfigureOptions(GraphQLOptions options, IServiceProvider provider) {}
    }
}
