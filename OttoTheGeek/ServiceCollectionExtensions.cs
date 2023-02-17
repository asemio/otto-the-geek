using System;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOtto<TModel>(this IServiceCollection services, TModel model)
            where TModel : OttoModel
        {
            services.TryRegisterGraphQLServer();
            
            var ottoSchema = model.BuildOttoSchema(services);

            return services
                .AddScoped(ctx => {
                    var schema = new ModelSchema<TModel>(ottoSchema, ctx);

                    return schema;
                })
                .AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>()
                .AddSingleton<DataLoaderDocumentListener>()
                ;
        }

        private static IServiceCollection TryRegisterGraphQLServer(this IServiceCollection services)
        {
            // using IDocumentExecuter to check for registration already present
            if (!services.Any(x => x.ServiceType == typeof(GraphQL.IDocumentExecuter)))
            {
                services
                    .AddGraphQL(builder =>
                    {
                        builder
                            .AddSystemTextJson()
                            .AddGraphTypes()
                            .AddDataLoader()
                            .ConfigureExecutionOptions(opts =>
                            {
                                opts.EnableMetrics = false;
                            })
                            ;
                    });
                services
                    .AddSingleton<IDocumentExecuter, Internal.OttoDocumentExecuter>()
                    ;
            }

            return services;
        }
    }
}
