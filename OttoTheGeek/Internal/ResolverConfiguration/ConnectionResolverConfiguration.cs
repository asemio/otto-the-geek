using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class ConnectionResolverConfiguration<TModel, TArgs, TResolver> : FieldWithArgsResolverConfiguration<TArgs>
        where TResolver : class, IConnectionResolver<TModel, TArgs>
        where TArgs : PagingArgs<TModel>
    {
        public override Type CoreClrType => typeof(Connection<TModel>);
        public override Type ConnectionType => typeof(Connection<TModel>);
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<Connection<TModel>>(services);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ResolverProxy : ResolverProxyBase<Connection<TModel>>
        {
            protected override Task<Connection<TModel>> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                var args = context.DeserializeArgs<TArgs>();

                return resolver.Resolve(args);
            }
        }
    }
}
