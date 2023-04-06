using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public sealed class ScalarResolverConfiguration<TResolver, TProp> : FieldResolverConfiguration
        where TResolver : class, ILooseScalarFieldResolver<TProp>
    {
        public override Type CoreClrType => typeof(TProp);

        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ScalarQueryFieldResolverProxy();
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        private sealed class ScalarQueryFieldResolverProxy : ResolverProxyBase<TProp>
        {
            protected override Task<TProp> Resolve(IResolveFieldContext context, IServiceProvider provider)
            {
                var resolver = provider.GetRequiredService<TResolver>();

                return resolver.Resolve();
            }
        }
    }
}
