using System;
using System.Collections.Generic;
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
        public abstract Type CoreClrType { get; }
        public virtual Type ConnectionType => null;
        public abstract void RegisterResolver(IServiceCollection services);

        public abstract IFieldResolver CreateGraphQLResolver();

        protected abstract IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services);

        protected virtual QueryArguments GetQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            return null;
        }
        
        public FieldType ConfigureField(PropertyInfo prop, GraphTypeCache cache, IServiceCollection services)
        {
            RegisterResolver(services);

            var graphType = GetGraphType(cache, services);

            var (t, resT) = GetGraphTypeConfiguration(graphType);

            return new FieldType {
                Name = prop.Name,
                Resolver = CreateGraphQLResolver(),
                Arguments = GetQueryArguments(cache, services),
                Type = t,
                ResolvedType = resT,
            };
        }

        public FieldType ConfigureField(PropertyInfo prop,
            OttoSchemaConfig config,
            IGraphType coreGraphType,
            Dictionary<Type, IInputObjectGraphType> inputGraphTypes
            )
        {
            var graphType = TransformGraphType(coreGraphType);
            var (t, resT) = GetGraphTypeConfiguration(graphType);
            
            return new FieldType {
                Name = prop.Name,
                Resolver = CreateGraphQLResolver(),
                Type = t,
                ResolvedType = resT,
            };
        }

        protected virtual IGraphType TransformGraphType(IGraphType coreGraphType)
        {
            return coreGraphType;
        }

        private (Type, IGraphType) GetGraphTypeConfiguration(IGraphType graphType)
        {
            var unresolvedType = OverrideForScalarList(graphType);

            if (unresolvedType == null)
            {
                return (null, graphType);
            }

            return (unresolvedType, null);
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
