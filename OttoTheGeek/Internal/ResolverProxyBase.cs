using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public abstract class ResolverProxyBase<T> : IFieldResolver<Task<T>>
    {
        public Task<T> Resolve(IResolveFieldContext context)
        {
            var resolver = ((IServiceProvider)context.Schema);

            return Resolve(context, resolver);
        }

        protected abstract Task<T> Resolve(IResolveFieldContext context, IServiceProvider provider);


        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}