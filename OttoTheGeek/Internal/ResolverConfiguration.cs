using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;

namespace OttoTheGeek.Internal
{
    public abstract class ResolverConfiguration
    {
        public abstract void RegisterResolver(IServiceCollection services);

        public abstract IFieldResolver CreateGraphQLResolver();

        public abstract IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services);

        public virtual QueryArguments GetQueryArguments()
        {
            return null;
        }
    }

    public sealed class ConnectionResolverConfiguration<TModel, TResolver> : ResolverConfiguration
        where TResolver : class, IConnectionResolver<TModel>
    {
        public override IFieldResolver CreateGraphQLResolver()
        {
            return new ResolverProxy();
        }

        public override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreate<Connection<TModel>>(services);
        }

        public override void RegisterResolver(IServiceCollection services)
        {
            services.AddTransient<TResolver>();
        }

        public override QueryArguments GetQueryArguments()
        {
            return new QueryArguments(
                new QueryArgument(typeof(NonNullGraphType<IntGraphType>)) { Name = nameof(PagingArgs.Count) },
                new QueryArgument(typeof(NonNullGraphType<IntGraphType>)) { Name = nameof(PagingArgs.Offset) }
            );
        }

        private sealed class ResolverProxy : ResolverProxyBase<Connection<TModel>>
        {
            protected override Task<Connection<TModel>> Resolve(ResolveFieldContext context, IDependencyResolver dependencyResolver)
            {
                var resolver = dependencyResolver.Resolve<TResolver>();
                var args = new PagingArgs {
                    Count = context.GetArgument<int>(nameof(PagingArgs.Count).ToCamelCase()),
                    Offset = context.GetArgument<int>(nameof(PagingArgs.Offset).ToCamelCase()),
                };
                return resolver.Resolve(args);
            }
        }
    }
}