using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;

namespace OttoTheGeek.Internal
{
    public sealed class ConnectionResolverConfiguration<TModel, TResolver> : FieldWithArgsResolverConfiguration<PagingArgs>
        where TResolver : class, IConnectionResolver<TModel>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<Connection<TModel>>(services);
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<Connection<TModel>>
        {
            protected override Task<Connection<TModel>> Resolve(ResolveFieldContext context, IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<TResolver>();

                return resolver.Resolve(context.DeserializeArgs<PagingArgs>());
            }
        }
    }
}