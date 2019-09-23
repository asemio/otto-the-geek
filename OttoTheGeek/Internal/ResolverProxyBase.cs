using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public abstract class ResolverProxyBase<T> : IFieldResolver<Task<T>>
    {
        public Task<T> Resolve(ResolveFieldContext context)
        {
            // this cast to Schema is gross...
            var resolver = ((Schema)context.Schema).DependencyResolver;

            return Resolve(context, resolver);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
        protected abstract Task<T> Resolve(ResolveFieldContext context, GraphQL.IDependencyResolver dependencyResolver);
    }
}