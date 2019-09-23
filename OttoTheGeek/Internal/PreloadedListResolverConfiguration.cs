using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class PreloadedListResolverConfiguration<TRecord> : ResolverConfiguration
    {
        public override IFieldResolver CreateGraphQLResolver()
        {
            return null;
        }

        public override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return new ListGraphType(cache.GetOrCreate<TRecord>(services));
        }

        public override void RegisterResolver(IServiceCollection services)
        {
        }
    }
}