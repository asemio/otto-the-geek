using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OttoTheGeek.Internal;

namespace OttoTheGeek.TypeModel;

public sealed class ReachabilityMap
{
    public ReachabilityMap(OttoSchemaConfig config)
    {
        var outputTypes = new Dictionary<Type, OttoTypeConfig>();
        
        AddReachableTypes(config.GetOrCreateBuilder(config.QueryClrType).TypeConfig, config, outputTypes);
        AddReachableTypes(config.GetOrCreateBuilder(config.MutationClrType).TypeConfig, config, outputTypes);

        OutputTypes = outputTypes.ToImmutableDictionary();
    }
    
    public ImmutableDictionary<Type, OttoTypeConfig> OutputTypes { get; }

    private void AddReachableTypes(OttoTypeConfig typeConfig, OttoSchemaConfig config, Dictionary<Type, OttoTypeConfig> existingTypes)
    {
        if (existingTypes.ContainsKey(typeConfig.ClrType))
        {
            return;
        }

        existingTypes[typeConfig.ClrType] = typeConfig;

        foreach (var field in typeConfig.GetRelevantProperties())
        {
            if (config.Scalars.IsScalarOrEnumerableOfScalar(field.PropertyType))
            {
                continue;
            }

            if (field.PropertyType.IsEnum)
            {
                continue;
            }

            var fieldConfig = typeConfig.Fields[field.Name];
            var connexType = fieldConfig.ResolverConfiguration?.ConnectionType;

            if (connexType != null)
            {
                AddReachableTypes(config.GetOrCreateBuilder(connexType).TypeConfig, config, existingTypes);
            }
                
            var coreType = field.PropertyType.GetEnumerableElementType() ?? field.PropertyType;
            
            AddReachableTypes(config.GetOrCreateBuilder(coreType).TypeConfig, config, existingTypes);

            if (coreType.IsInterface)
            {
                var implementations = config.LegacyBuilders.Values
                    .Select(x => x.TypeConfig)
                    .Where(x => x.Interfaces.Contains(coreType));

                foreach (var impl in implementations)
                {
                    AddReachableTypes(impl, config, existingTypes);
                }
            }
        }
    }
}
