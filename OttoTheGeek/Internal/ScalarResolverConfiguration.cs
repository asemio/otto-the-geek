using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class ScalarResolverConfiguration<TResolver, TProp> : FieldResolverConfiguration
        where TResolver : class, IScalarFieldResolver<TProp>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ScalarQueryFieldResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        protected override void RegisterResolver(IServiceCollection services)
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