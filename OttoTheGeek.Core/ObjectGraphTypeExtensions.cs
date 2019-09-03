using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace OttoTheGeek.Core
{
    public static class ObjectGraphTypeExtensions
    {
        private static readonly IReadOnlyDictionary<Type, Type> CSharpToGraphqlTypeMapping = new Dictionary<Type, Type>{
            { typeof(string), typeof(NonNullGraphType<StringGraphType>) },
            { typeof(int), typeof(NonNullGraphType<IntGraphType>) },
            { typeof(long), typeof(NonNullGraphType<IntGraphType>) },
            { typeof(long?), typeof(IntGraphType) },
        };

        public static void RegisterProperties<TModel>(this ObjectGraphType queryType, TModel model)
        {
            foreach(var prop in typeof(TModel).GetProperties())
            {
                if(CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphQlType))
                {
                    queryType.Field(
                        type: graphQlType,
                        name: prop.Name,
                        resolve: ctx => prop.GetValue(model)
                    );
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
        }

        public static void RegisterProperties<TQuery>(this ObjectGraphType queryType, TQuery rootObject, SchemaBuilder<TQuery> builder)
        {
            foreach(var prop in typeof(TQuery).GetProperties())
            {
                if(CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphQlType))
                {
                    queryType.Field(
                        type: graphQlType,
                        name: prop.Name,
                        resolve: ctx => prop.GetValue(rootObject)
                    );
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
        }
    }
}