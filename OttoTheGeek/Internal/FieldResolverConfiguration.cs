using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public abstract class FieldResolverConfiguration
    {
        protected abstract void RegisterResolver(IServiceCollection services);

        protected abstract IFieldResolver CreateGraphQLResolver();

        protected abstract IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services);

        protected virtual QueryArguments GetQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            return null;
        }

        public FieldType ConfigureField(PropertyInfo prop, GraphTypeCache cache, IServiceCollection services)
        {
            RegisterResolver(services);

            return new FieldType {
                Name = prop.Name,
                ResolvedType = GetGraphType(cache, services),
                Type = prop.PropertyType,
                Resolver = CreateGraphQLResolver(),
                Arguments = GetQueryArguments(cache, services)
            };
        }
    }
}