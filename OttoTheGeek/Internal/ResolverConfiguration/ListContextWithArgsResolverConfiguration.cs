using System;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    internal sealed class ListContextWithArgsResolverConfiguration<TResolver, TModel, TField, TArgs> : FieldWithArgsResolverConfiguration<TArgs>
        where TResolver : class, IListFieldWithArgsResolver<TModel, TField, TArgs>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return new ListGraphType(cache.GetOrCreate<TField>(services));
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

                var args = context.DeserializeArgs<TArgs>();
                var loader = loaderContext.GetOrAddCollectionBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys, args));

                return loader.LoadAsync(resolver.GetKey((TModel) context.Source));
            }
        }
    }
}
