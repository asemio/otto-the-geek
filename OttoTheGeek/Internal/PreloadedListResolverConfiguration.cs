using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class PreloadedListResolverConfiguration<TRecord> : FieldResolverConfiguration
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return null;
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return new ListGraphType(cache.GetOrCreate(typeof(TRecord), services));
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
        }
    }
}