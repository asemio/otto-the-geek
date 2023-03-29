using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class ScalarResolverConfiguration<TResolver, TProp> : FieldResolverConfiguration
        where TResolver : class, ILooseScalarFieldResolver<TProp>
    {
        public override Type CoreClrType => typeof(TProp);

        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ScalarQueryFieldResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ScalarQueryFieldResolverProxy : ResolverProxyBase<TProp>
        {
            protected override Task<TProp> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                return resolver.Resolve();
            }
        }
    }
}
