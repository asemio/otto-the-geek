using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class LooseListResolverConfiguration<TResolver, TElem> : FieldResolverConfiguration
        where TResolver : class, ILooseListFieldResolver<TElem>
    {
        public override Type CoreClrType => typeof(TElem);

        private readonly ScalarTypeMap _scalarTypeMap;

        public LooseListResolverConfiguration(ScalarTypeMap scalarTypeMap)
        {
            _scalarTypeMap = scalarTypeMap;
        }

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            if(_scalarTypeMap.TryGetGraphType(typeof(TElem), out var scalarGraphType))
            {
                var listType = typeof(ListGraphType<>).MakeGenericType(scalarGraphType);

                return (IGraphType)Activator.CreateInstance(listType);
            }

            return new ListGraphType(cache.GetOrCreate<TElem>(services));
        }

        protected override IGraphType TransformGraphType(IGraphType coreGraphType)
        {
            return new ListGraphType(coreGraphType);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<IEnumerable<TElem>>
        {
            protected override Task<IEnumerable<TElem>> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                return resolver.Resolve();
            }
        }
    }
}
