using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    internal sealed class ListContextResolverConfiguration<TResolver, TModel, TField> : FieldResolverConfiguration
        where TResolver : class, IListFieldResolver<TModel, TField>
    {
        public override Type CoreClrType { get; }

        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return new ListGraphType(cache.GetOrCreate<TField>(services));
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : IFieldResolver
        {
            public ValueTask<object> ResolveAsync(IResolveFieldContext context)
            {
                var provider = ((IServiceProvider)context.Schema);
                var loaderContext = provider.GetRequiredService<IDataLoaderContextAccessor>().Context;
                var resolver = provider.GetRequiredService<TResolver>();

                var loader = loaderContext.GetOrAddCollectionBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                return new ValueTask<object>(loader.LoadAsync(resolver.GetKey((TModel) context.Source)));
            }
        }
    }
}
