using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQL.Types;
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
            var reachabilityMap = new ReachabilityMap(config);

            var outputGraphTypes = typeMap
                .Where(x => reachabilityMap.OutputTypes.ContainsKey(x.Key))
                .Select(x => KeyValuePair.Create(x.Key, x.Value.ToGqlNetGraphType(config)))
                .ToDictionary(x => x.Key, x => x.Value);
            
            ValidateNoDuplicates(outputGraphTypes);

            var inputGraphTypes = reachabilityMap.InputTypes
                .Select(x => KeyValuePair.Create(x.Key, x.Value.ToGqlNetInputGraphType(config)))
                .ToDictionary(x => x.Key, x => x.Value);

            var interfaceImpls = new HashSet<Type>();

            foreach (var t in inputGraphTypes.Keys)
            {
                var graphType = inputGraphTypes[t];
                var typeConfig = config.GetOrCreateBuilder(t).TypeConfig;

                foreach (var field in typeConfig.GetRelevantFieldConfigs())
                {
                    graphType.AddField(field.ToGqlNetInputField(config, inputGraphTypes));
                }
            }
            
            foreach (var t in outputGraphTypes.Keys)
            {
                var graphType = outputGraphTypes[t];
                var typeConfig = typeMap[t];
                
                foreach (var f in typeConfig.Fields.Values)
                {
                    graphType.AddField(f.ToGqlNetField(config, outputGraphTypes, inputGraphTypes));
                }
                
                foreach (var iface in typeConfig.Interfaces)
                {
                    ((IObjectGraphType)graphType).AddResolvedInterface((IInterfaceGraphType)outputGraphTypes[iface]);
                    interfaceImpls.Add(t);
                }

            }

            var queryType = outputGraphTypes[config.QueryClrType];
            if (queryType.Fields.Any())
            {
                Query = (IObjectGraphType)queryType;
            }
            var mutationType = outputGraphTypes[config.MutationClrType];
            if (mutationType.Fields.Any())
            {
                Mutation = (IObjectGraphType)mutationType;
            }

            foreach (var impl in interfaceImpls)
            {
                var implGraphType = (ObjectGraphType)outputGraphTypes[impl];
                implGraphType.IsTypeOf = x => impl.IsAssignableFrom(x?.GetType());
                RegisterType(implGraphType);
            }
        }
        
        private ImmutableDictionary<Type, OttoTypeConfig> GetTypeMap(OttoSchemaConfig config)
        {
            var visited = new HashSet<Type>();

            var map = config.LegacyBuilders
                .ToImmutableDictionary(x => x.Key, x => x.Value.TypeConfig);

            map = UpdateMap(config.QueryClrType, map, visited, config.Scalars);
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
            
            var config = map.GetValueOrDefault(t, OttoTypeConfig.ForOutputType(t));
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
                map = UpdateMap(field.ResolverConfiguration.CoreClrType, map, visited, scalars);
            }

            return map;
        }
        
        private void ValidateNoDuplicates(Dictionary<Type, IComplexGraphType> graphTypes)
        {
            var groups = graphTypes
                .GroupBy(x => x.Value.Name)
                .Where(x => x.Count() > 1);

            var group = groups.FirstOrDefault();

            if(group == null)
            {
                return;
            }

            throw new DuplicateTypeNameException(group.Key, group.Select(x => x.Key));
        }
    }
}
