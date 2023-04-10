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
            var provider = context.RequestServices;

            return await Resolve(context, provider);
        }

        protected abstract Task<T> Resolve(IResolveFieldContext context, IServiceProvider provider);
    }
}
