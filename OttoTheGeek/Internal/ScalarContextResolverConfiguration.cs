using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    internal sealed class ScalarContextResolverConfiguration<TResolver, TModel, TChild> : FieldResolverConfiguration
        where TResolver : class, IScalarFieldResolver<TModel, TChild>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TChild>(services);
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<TChild>
        {
            protected override Task<TChild> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var loaderContext = provider.GetRequiredService<IDataLoaderContextAccessor>().Context;
                var resolver = provider.GetRequiredService<TResolver>();

                var loader = loaderContext.GetOrAddBatchLoader<object, TChild>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                var loaderResult = loader.LoadAsync(resolver.GetKey((TModel)context.Source));

                return loaderResult.GetResultAsync();
            }
        }
    }
}