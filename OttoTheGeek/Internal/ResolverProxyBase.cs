using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;

namespace OttoTheGeek.Internal
{
    public abstract class ResolverProxyBase<T> : IFieldResolver
    {
        public async ValueTask<object> ResolveAsync(IResolveFieldContext context)
        {
            var resolver = ((IServiceProvider)context.Schema);

            return await Resolve(context, resolver);
        }

        protected abstract Task<T> Resolve(IResolveFieldContext context, IServiceProvider provider);
    }
}
