using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.Authorization
{
    internal abstract class AuthResolverStub
    {
        public abstract IFieldResolver GetResolver(IServiceCollection services, IFieldResolver wrapped);
        public abstract void ValidateGraphqlType(Type t, PropertyInfo prop);
    }

    internal sealed class NullAuthResolverStub : AuthResolverStub
    {
        public override IFieldResolver GetResolver(IServiceCollection services, IFieldResolver wrapped)
        {
            return wrapped;
        }

        public override void ValidateGraphqlType(Type t, PropertyInfo prop) { }
    }

    internal sealed class AuthResolverStub<TAuthorizer> : AuthResolverStub
        where TAuthorizer : class
    {
        private readonly Func<TAuthorizer, Task<bool>> _cb;

        public AuthResolverStub(Func<TAuthorizer, bool> cb)
        {
            _cb = (x => Task.FromResult(cb(x)));
        }

        public AuthResolverStub(Func<TAuthorizer, Task<bool>> cb)
        {
            _cb = cb;
        }

        public override IFieldResolver GetResolver(IServiceCollection services, IFieldResolver wrapped)
        {
            services.AddTransient<TAuthorizer>();

            return new AuthResolver<TAuthorizer>(_cb, wrapped);
        }

        public override void ValidateGraphqlType(Type t, PropertyInfo prop)
        {
            if(t.IsGenericFor(typeof(NonNullGraphType<>)))
            {
                throw new AuthorizationConfigurationException(prop);
            }

        }
    }
}