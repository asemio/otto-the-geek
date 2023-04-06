using System;
using System.Linq;
using GraphQL;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOtto<TModel>(
            this IServiceCollection services,
            TModel model,
            Action<IGraphQLBuilder> configureAction = null
            )
            where TModel : OttoModel
        {
            var schemaConfig = model.BuildConfig();
            schemaConfig.RegisterResolvers(services);
            
            services
                .AddGraphQL(builder =>
                {
                    builder
                        .AddSystemTextJson()
                        .AddGraphTypes()
                        .AddDataLoader()
                        .AddSchema(sp => new ModelSchema<TModel>(schemaConfig, sp))
                        .AddExecutionStrategy<SerialExecutionStrategy>(OperationType.Query)
                        .ConfigureExecutionOptions(opts =>
                        {
                            opts.EnableMetrics = false;
                        })
                        ;
                    configureAction?.Invoke(builder);
                });
            
            services
                .AddSingleton<IDocumentExecuter, Internal.OttoDocumentExecuter>()
                ;

            return services;
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
