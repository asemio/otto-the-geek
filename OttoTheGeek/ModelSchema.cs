using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQL.Types;
using OttoTheGeek.Internal;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek
{
    public class ModelSchema<TModel> : Schema
    {
        public ModelSchema(OttoSchemaInfo schema, IServiceProvider provider)
            : base(provider)
        {
            if(schema.QueryType?.Fields?.Any() == true)
            {
                Query = schema.QueryType;
            }
            if(schema.MutationType?.Fields?.Any() == true)
            {
                Mutation = schema.MutationType;
            }
            if(schema.SubscriptionType?.Fields?.Any() == true)
            {
                Subscription = schema.SubscriptionType;
            }

            foreach (var gt in schema.OtherTypes)
            {
                RegisterType(gt);
            }
        }

        public ModelSchema(OttoSchemaConfig config, IServiceProvider provider) : base(provider)
        {
            var typeMap = GetTypeMap(config);

            var graphTypes = typeMap
                .Select(x => KeyValuePair.Create(x.Key, x.Value.ToGqlNetGraphType(config)))
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var t in typeMap.Keys)
            {
                var graphType = graphTypes[t];
                var typeConfig = typeMap[t];

                foreach (var f in typeConfig.Fields.Values)
                {
                    graphType.AddField(f.ToGqlNetField(config, graphTypes));
                }
            }

            var queryType = graphTypes[config.QueryClrType];
            if (queryType.Fields.Any())
            {
                Query = (IObjectGraphType)queryType;
            }
            var mutationType = graphTypes[config.MutationClrType];
            if (mutationType.Fields.Any())
            {
                Mutation = (IObjectGraphType)mutationType;
            }
        }
        
        private ImmutableDictionary<Type, OttoTypeConfig> GetTypeMap(OttoSchemaConfig config)
        {
            var visited = new HashSet<Type>();

            var map = UpdateMap(config.QueryClrType, config.Types, visited, config.Scalars);
            map = UpdateMap(config.MutationClrType, map, visited, config.Scalars);

            return map;
        }

        private ImmutableDictionary<Type, OttoTypeConfig> UpdateMap(Type t, ImmutableDictionary<Type, OttoTypeConfig> map, HashSet<Type> visited, OttoScalarTypeMap scalars)
        {
            if (scalars.Map.ContainsKey(t))
            {
                return map;
            }
            
            if (visited.Contains(t))
            {
                return map;
            }

            visited.Add(t);
            
            var config = map.GetValueOrDefault(t, OttoTypeConfig.ForType(t));
            if (!map.ContainsKey(t))
            {
                map = map.Add(t, config);
            }

            foreach (var field in config.Fields.Values)
            {
                if (field.ResolverConfiguration == null)
                {
                    continue;
                }
                map = UpdateMap(field.ResolverConfiguration.ClrType, map, visited, scalars);
            }

            return map;
        }
    }
}
