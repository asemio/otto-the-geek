using System;
using System.Reflection;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal.ResolverConfiguration
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

            var graphType = GetGraphType(cache, services);

            var unresolvedType = OverrideForScalarList(graphType);

            return new FieldType {
                Name = prop.Name,
                Type = unresolvedType,
                ResolvedType = unresolvedType == null ? graphType : null,
                Resolver = CreateGraphQLResolver(),
                Arguments = GetQueryArguments(cache, services)
            };
        }

        private Type OverrideForScalarList(IGraphType graphType)
        {
            var graphTypeType = graphType.GetType();

            if(!graphTypeType.IsGenericFor(typeof(ListGraphType<>)))
            {
                return null;
            }

            var namedElemType = graphTypeType.GetNamedType();

            if(namedElemType == null)
            {
                return null;
            }

            if(typeof(ScalarGraphType).IsAssignableFrom(namedElemType))
            {
                return graphTypeType;
            }

            return null;
        }
    }
}
