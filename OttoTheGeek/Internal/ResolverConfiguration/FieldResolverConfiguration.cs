using System;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public abstract class FieldResolverConfiguration
    {
        public abstract Type CoreClrType { get; }
        public virtual Type ConnectionType => null;
        public abstract void RegisterResolver(IServiceCollection services);

        public abstract IFieldResolver CreateGraphQLResolver();
    }
}
