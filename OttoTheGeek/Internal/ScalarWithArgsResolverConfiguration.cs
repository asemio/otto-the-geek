using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class ScalarWithArgsResolverConfiguration<TResolver, TProp, TArgs> : FieldWithArgsResolverConfiguration<TArgs>
        where TResolver : class, IScalarFieldWithArgsResolver<TProp, TArgs>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<TProp>
        {
            protected override Task<TProp> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<TResolver>();

                return resolver.Resolve(context.DeserializeArgs<TArgs>());
            }
        }
    }
}