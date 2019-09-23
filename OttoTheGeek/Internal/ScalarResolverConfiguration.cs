using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class ScalarResolverConfiguration<TResolver, TProp> : ResolverConfiguration
        where TResolver : class, IScalarFieldResolver<TProp>
    {
        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ScalarQueryFieldResolverProxy();
        }

        public override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ScalarQueryFieldResolverProxy : ResolverProxyBase<TProp>
        {
            protected override Task<TProp> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<TResolver>();

                return resolver.Resolve();
            }
        }
    }
}