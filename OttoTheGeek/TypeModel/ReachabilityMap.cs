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
        var inputTypes = new Dictionary<Type, OttoTypeConfig>();
        var argumentsTypes = new HashSet<Type>();
        
        AddReachableOutputTypes(config.GetOrCreateBuilder(config.QueryClrType).TypeConfig, config, outputTypes, argumentsTypes);
        AddReachableOutputTypes(config.GetOrCreateBuilder(config.MutationClrType).TypeConfig, config, outputTypes, argumentsTypes);

        foreach (var t in argumentsTypes)
        {
            AddInputTypes(t, config, inputTypes);
        }
        
        OutputTypes = outputTypes.ToImmutableDictionary();
        InputTypes = inputTypes.ToImmutableDictionary();
    }
    
    public ImmutableDictionary<Type, OttoTypeConfig> OutputTypes { get; }
    public ImmutableDictionary<Type, OttoTypeConfig> InputTypes { get; }

    private void AddReachableOutputTypes(OttoTypeConfig typeConfig, OttoSchemaConfig config,
        Dictionary<Type, OttoTypeConfig> existingTypes, HashSet<Type> argumentsTypes)
    {
        if (existingTypes.ContainsKey(typeConfig.ClrType))
        {
            return;
        }

        existingTypes[typeConfig.ClrType] = typeConfig;

        foreach (var field in typeConfig.GetRelevantProperties())
        {
            var fieldConfig = typeConfig.Fields[field.Name];
            if (fieldConfig.ArgumentsType != null)
            {
                argumentsTypes.Add(fieldConfig.ArgumentsType);
            }
            
            var coreType = field.PropertyType.UnwrapNullableAndEnumerable();
            if (fieldConfig.IsScalarLike(config))
            {
                continue;
            }

            var connexType = fieldConfig.ResolverConfiguration?.ConnectionType;

            if (connexType != null)
            {
                AddReachableOutputTypes(config.GetOrCreateBuilder(connexType).TypeConfig, config, existingTypes, argumentsTypes);
            }
            
            AddReachableOutputTypes(config.GetOrCreateBuilder(coreType).TypeConfig, config, existingTypes, argumentsTypes);

            if (coreType.IsInterface)
            {
                var implementations = config.LegacyBuilders.Values
                    .Select(x => x.TypeConfig)
                    .Where(x => x.Interfaces.Contains(coreType));

                foreach (var impl in implementations)
                {
                    AddReachableOutputTypes(impl, config, existingTypes, argumentsTypes);
                }
            }
        }
    }

    private void AddInputTypes(Type parent, OttoSchemaConfig config, Dictionary<Type, OttoTypeConfig> existingInputTypes)
    {
        if (existingInputTypes.ContainsKey(parent))
        {
            return;
        }

        var typeConfig = config.GetOrCreateBuilder(parent).TypeConfig;

        existingInputTypes[parent] = typeConfig;
        
        var fields = typeConfig.GetRelevantFieldConfigs()
            .Where(x => !config.Scalars.IsScalarOrEnumerableOfScalar(x.Property.PropertyType))
            .Where(x => !typeof(OrderValue).IsAssignableFrom(x.Property.PropertyType));

        foreach (var field in fields)
        {
            var coreType = field.Property.PropertyType.UnwrapNullableAndEnumerable();
            AddInputTypes(coreType, config, existingInputTypes);
        }
    }
}
