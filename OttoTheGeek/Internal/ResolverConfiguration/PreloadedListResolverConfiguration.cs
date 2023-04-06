using System;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class PreloadedListResolverConfiguration<TModel, TProp> : FieldResolverConfiguration
    {
        public override Type CoreClrType => typeof(TProp);

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new PreloadedFieldResolver<TModel>();
        }

        public override void RegisterResolver(IServiceCollection services)
        {
        }
    }
}
