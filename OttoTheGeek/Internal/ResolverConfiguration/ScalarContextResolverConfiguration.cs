using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    internal sealed class ScalarContextResolverConfiguration<TResolver, TModel, TChild> : FieldResolverConfiguration
        where TResolver : class, IScalarFieldResolver<TModel, TChild>
    {
        public override Type CoreClrType => typeof(TChild);

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : IFieldResolver
        {
            public ValueTask<object> ResolveAsync(IResolveFieldContext context)
            {
                var provider = context.RequestServices;
                var loaderContext = provider.GetRequiredService<IDataLoaderContextAccessor>().Context;
                var resolver = provider.GetRequiredService<TResolver>();

                var loader = loaderContext.GetOrAddBatchLoader<object, TChild>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys));

                var key = resolver.GetKey((TModel) context.Source);
                if (key == null)
                {
                    return new ValueTask<object>(result: null);
                }

                return new ValueTask<object>(loader.LoadAsync(key));
            }
        }
    }
}
