using System;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace OttoTheGeek.Internal.Authorization
{
    public sealed class AuthResolver<TAuthorizer> : IFieldResolver
    {
        private readonly Func<TAuthorizer, bool> _authFn;
        private readonly IFieldResolver _wrapped;

        public AuthResolver(Func<TAuthorizer, bool> authFn, IFieldResolver wrapped)
        {
            _authFn = authFn;
            _wrapped = wrapped;
        }

        public object Resolve(ResolveFieldContext context)
        {
            var authorizer = ((Schema)context.Schema).DependencyResolver.Resolve<TAuthorizer>();
            if(_authFn(authorizer))
            {
                return _wrapped.Resolve(context);
            }

            context.Errors.Add(new GraphQL.ExecutionError("Not authorized"));
            return null;
        }
    }
}