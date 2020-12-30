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
            return Resolve(context, context.RequestServices);
        }

        protected abstract Task<T> Resolve(IResolveFieldContext context, IServiceProvider provider);


        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}