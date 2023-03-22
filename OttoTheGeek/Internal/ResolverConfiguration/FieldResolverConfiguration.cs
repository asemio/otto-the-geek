using System;
using System.Reflection;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek.Internal.ResolverConfiguration
{
    public abstract class FieldResolverConfiguration
    {
        public abstract Type ClrType { get; }
        public abstract void RegisterResolver(IServiceCollection services);

        protected abstract IFieldResolver CreateGraphQLResolver();

        protected abstract IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services);
        protected abstract IGraphType GetGraphType(OttoSchemaConfig config);

        protected virtual QueryArguments GetQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            return null;
        }
        
        protected virtual QueryArguments GetQueryArguments(OttoSchemaConfig config)
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

        public FieldType ConfigureField(PropertyInfo prop, OttoSchemaConfig config, IGraphType graphType)
        {
            var unresolvedType = OverrideForScalarList(graphType);
            
            return new FieldType {
                Name = prop.Name,
                Resolver = CreateGraphQLResolver(),
                Arguments = GetQueryArguments(config),
                Type = unresolvedType,
                ResolvedType = unresolvedType == null ? graphType : null,
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
