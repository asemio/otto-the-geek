using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.Authorization
{
    public sealed class AuthResolver<TAuthorizer> : IFieldResolver
    {
        private readonly Func<TAuthorizer, Task<bool>> _authFn;
        private readonly IFieldResolver _wrapped;

        public AuthResolver(Func<TAuthorizer, Task<bool>> authFn, IFieldResolver wrapped)
        {
            _authFn = authFn;
            _wrapped = wrapped;
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return ResolveCore(context);
        }

        private async Task<object> ResolveCore(IResolveFieldContext context)
        {
            var sp = ((IServiceProvider)context.Schema);
            var authorizer = sp.GetRequiredService<TAuthorizer>();
            if(await _authFn(authorizer))
            {
                var res = _wrapped.Resolve(context);

                if(res is Task t)
                {
                    await t.ConfigureAwait(false);

                    return ((dynamic)t).Result;
                }

                return res;
            }

            context.Errors.Add(new GraphQL.ExecutionError("Not authorized"));
            return null;
        }
    }
}