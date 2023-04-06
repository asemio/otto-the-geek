using System;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class PreloadedScalarResolverConfiguration<TModel, TProp> : FieldResolverConfiguration
    {
        public override Type CoreClrType => typeof(TProp);

        public override void RegisterResolver(IServiceCollection services)
        {
        }

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new PreloadedFieldResolver<TModel>();
        }
    }
}
