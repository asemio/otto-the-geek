using System;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
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

        private sealed class ResolverProxy : IFieldResolver
        {
            public object Resolve(IResolveFieldContext context)
            {
                var provider = ((IServiceProvider)context.Schema);
                var loaderContext = provider.GetRequiredService<IDataLoaderContextAccessor>().Context;
                var resolver = provider.GetRequiredService<TResolver>();

                var loader = loaderContext.GetOrAddBatchLoader<object, TChild>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                var key = resolver.GetKey((TModel) context.Source);
                if (key == null)
                {
                    return null;
                }

                return loader.LoadAsync(key);
            }
        }
    }
}
