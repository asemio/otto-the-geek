using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class ListResolverConfiguration<TResolver, TElem> : FieldResolverConfiguration
        where TResolver : class, ILooseListFieldResolver<TElem>
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return new ListGraphType(cache.GetOrCreate<TElem>(services));
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<IEnumerable<TElem>>
        {
            protected override Task<IEnumerable<TElem>> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                return resolver.Resolve();
            }
        }
    }
}