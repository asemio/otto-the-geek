using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.Authorization
{
    public abstract class AuthResolverStub
    {
        public abstract IFieldResolver GetResolver(IServiceCollection services, IFieldResolver wrapped);
        public abstract IFieldResolver GetResolver(IFieldResolver wrapped);
        public abstract void RegisterResolver(IServiceCollection services);
        public abstract void ValidateGraphqlType(Type t, PropertyInfo prop);
        public abstract void ValidateGraphqlType(IGraphType gt, PropertyInfo prop);
    }

    public sealed class NullAuthResolverStub : AuthResolverStub
    {
        public override IFieldResolver GetResolver(IServiceCollection services, IFieldResolver wrapped)
        {
            return GetResolver(wrapped);
        }

        public override IFieldResolver GetResolver(IFieldResolver wrapped)
        {
            return wrapped;
        }

        public override void RegisterResolver(IServiceCollection services)
        {
        }

        public override void ValidateGraphqlType(Type t, PropertyInfo prop) { }
        public override void ValidateGraphqlType(IGraphType gt, PropertyInfo prop) { }
    }

    public sealed class AuthResolverStub<TAuthorizer> : AuthResolverStub
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
            RegisterResolver(services);

            return GetResolver(wrapped);
        }

        public override IFieldResolver GetResolver(IFieldResolver wrapped)
        {
            return new AuthResolver<TAuthorizer>(_cb, wrapped);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TAuthorizer>();
        }

        public override void ValidateGraphqlType(Type t, PropertyInfo prop)
        {
            if(t.IsGenericFor(typeof(NonNullGraphType<>)))
            {
                throw new AuthorizationConfigurationException(prop);
            }

        }

        public override void ValidateGraphqlType(IGraphType gt, PropertyInfo prop)
        {
            if (gt is NonNullGraphType || gt.GetType().IsGenericFor(typeof(NonNullGraphType<>)))
            {
                throw new AuthorizationConfigurationException(prop);
            }
        }
    }
}
