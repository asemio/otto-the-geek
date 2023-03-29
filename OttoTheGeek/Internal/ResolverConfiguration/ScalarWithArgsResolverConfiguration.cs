using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class ScalarWithArgsResolverConfiguration<TResolver, TProp, TArgs> : FieldWithArgsResolverConfiguration<TArgs>
        where TResolver : class, ILooseScalarFieldWithArgsResolver<TProp, TArgs> where TArgs : class
    {
        public override Type CoreClrType => typeof(TProp);

        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<TProp>(services);
        }

        protected override QueryArguments GetQueryArguments(OttoSchemaConfig config,
            Dictionary<Type, IInputObjectGraphType> inputTypes)
        {
            return config.GetGqlNetArguments<TArgs>(inputTypes);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<TProp>
        {
            protected override Task<TProp> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                var args = context.DeserializeArgs<TArgs>();

                return resolver.Resolve(args);
            }
        }
    }
}
