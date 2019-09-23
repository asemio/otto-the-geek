using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public sealed class QueryFieldGraphqlResolverProxy<T> : GraphQL.Resolvers.IFieldResolver<Task<T>>
    {
        private readonly IScalarFieldResolver<T> _resolver;

        public QueryFieldGraphqlResolverProxy(IScalarFieldResolver<T> resolver)
        {
            _resolver = resolver;
        }
        public Task<T> Resolve(ResolveFieldContext context)
        {
            return _resolver.Resolve();
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}