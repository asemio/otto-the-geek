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

        public ValueTask<object> ResolveAsync(IResolveFieldContext context)
        {
            return new ValueTask<object>(_resolver.Resolve());
        }
    }
}
