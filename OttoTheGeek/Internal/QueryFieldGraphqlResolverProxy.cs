using System.Threading.Tasks;
using GraphQL;

namespace OttoTheGeek.Internal
{
    public sealed class QueryFieldGraphqlResolverProxy<T> : GraphQL.Resolvers.IFieldResolver
    {
        private readonly ILooseScalarFieldResolver<T> _resolver;

        public QueryFieldGraphqlResolverProxy(ILooseScalarFieldResolver<T> resolver)
        {
            _resolver = resolver;
        }

        public async ValueTask<object> ResolveAsync(IResolveFieldContext context)
        {
            return await _resolver.Resolve();
        }
    }
}
