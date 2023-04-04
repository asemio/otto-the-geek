using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    internal sealed class ScalarContextWithArgsResolverConfiguration<TResolver, TModel, TField, TArgs> : FieldWithArgsResolverConfiguration<TArgs>
        where TResolver : class, IScalarFieldWithArgsResolver<TModel, TField, TArgs>
    {
        public override Type CoreClrType => typeof(TField);

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TField>(services);
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

                var args = context.DeserializeArgs<TArgs>();
                var loader = loaderContext.GetOrAddBatchLoader<object, TField>(resolver.GetType().FullName, async (keys, token) => await resolver.GetData(keys, args));

                return new ValueTask<object>(loader.LoadAsync(resolver.GetKey((TModel)context.Source)));
            }
        }
    }
}
