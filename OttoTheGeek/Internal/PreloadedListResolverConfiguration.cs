using System;
using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class PreloadedListResolverConfiguration<TRecord> : FieldResolverConfiguration
    {
        protected override IFieldResolver CreateGraphQLResolver()
        {
            return null;
        }

        protected override IGraphType GetGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            if(ScalarTypeMap.TryGetGraphType(typeof(TRecord), out var scalarGraphType))
            {
                var listGraphType = typeof(ListGraphType<>).MakeGenericType(scalarGraphType);
                var listTypeInstance = (ListGraphType)Activator.CreateInstance(listGraphType);
                listTypeInstance.ResolvedType = (IGraphType)Activator.CreateInstance(scalarGraphType);
                if(listTypeInstance.ResolvedType is NonNullGraphType elemTypeInstance)
                {
                    elemTypeInstance.ResolvedType = (IGraphType)Activator.CreateInstance(scalarGraphType.GetGenericArguments().Single());
                }

                return listTypeInstance;
            }

            return new ListGraphType(cache.GetOrCreate(typeof(TRecord), services));
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
        }
    }
}