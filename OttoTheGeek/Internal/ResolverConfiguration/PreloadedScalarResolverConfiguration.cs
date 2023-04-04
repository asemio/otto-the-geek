using System;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class PreloadedScalarResolverConfiguration<TModel, TProp> : FieldResolverConfiguration
    {
        public override Type CoreClrType => typeof(TProp);

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
        }

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new PreloadedFieldResolver<TModel>();
        }
    }
}
