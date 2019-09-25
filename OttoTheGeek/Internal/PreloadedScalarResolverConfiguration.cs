using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class PreloadedScalarResolverConfiguration<TRecord> : FieldResolverConfiguration
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return null;
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TRecord>(services);
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
        }
    }
}