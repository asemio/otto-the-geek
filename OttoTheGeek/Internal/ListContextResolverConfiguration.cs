using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    internal sealed class ListContextResolverConfiguration<TResolver, TModel, TField> : FieldResolverConfiguration
        where TResolver : class, IListFieldResolver<TModel, TField>
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

        private sealed class ResolverProxy : ResolverProxyBase<IEnumerable<TField>>
        {
            protected override Task<IEnumerable<TField>> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var loaderContext = dependencyResolver.Resolve<IDataLoaderContextAccessor>().Context;
                var resolver = dependencyResolver.Resolve<TResolver>();

                var loader = loaderContext.GetOrAddCollectionBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                return loader.LoadAsync(resolver.GetKey((TModel)context.Source));
            }
        }
    }
}