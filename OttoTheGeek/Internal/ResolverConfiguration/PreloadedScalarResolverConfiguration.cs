using System;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class PreloadedScalarResolverConfiguration<TModel, TProp> : FieldResolverConfiguration
    {
        public override Type ClrType => typeof(TProp);

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        protected override IGraphType GetGraphType(OttoSchemaConfig config)
        {
            throw new System.NotImplementedException();
        }

        public override void RegisterResolver(IServiceCollection services)
        {
        }

        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new PreloadedFieldResolver<TModel>();
        }
    }
}
