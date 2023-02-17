using System;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOtto<TModel>(this IServiceCollection services, TModel model, Action<IGraphQLBuilder> configureAction = null)
            where TModel : OttoModel
        {
            services.TryRegisterGraphQLServer(configureAction);
            
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

        private static IServiceCollection TryRegisterGraphQLServer(this IServiceCollection services, Action<IGraphQLBuilder> configureAction)
        {
            configureAction ??= NopConfigure;
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
                            .AddExecutionStrategy<SerialExecutionStrategy>(OperationType.Query)
                            .ConfigureExecutionOptions(opts =>
                            {
                                opts.EnableMetrics = false;
                            })
                            ;
                        configureAction(builder);
                    });
                services
                    .AddSingleton<IDocumentExecuter, Internal.OttoDocumentExecuter>()
                    ;
            }

            return services;
        }

        private static void NopConfigure(IGraphQLBuilder builder)
        {
        }
    }
}
