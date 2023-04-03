using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek.TypeModel;

public record OttoSchemaConfig(
    Type QueryClrType,
    Type MutationClrType,
    OttoScalarTypeMap Scalars,
    [property: Obsolete]
    ImmutableDictionary<Type, IGraphTypeBuilder> LegacyBuilders,
    [property: Obsolete]
    ScalarTypeMap LegacyScalars
    )
{
    public void RegisterResolvers(IServiceCollection services)
    {
        var resolverConfigs = LegacyBuilders.Values
            .Select(x => x.TypeConfig)
            .SelectMany(x => x.Fields)
            .Select(x => x.Value.ResolverConfiguration)
            .Where(x => x != null);

        foreach (var r in resolverConfigs)
        {
            r.RegisterResolver(services);
        }
        
        var authResolvers = LegacyBuilders.Values
            .Select(x => x.TypeConfig)
            .SelectMany(x => x.Fields)
            .Select(x => x.Value.AuthResolver)
            .Where(x => x != null);

        foreach (var r in authResolvers)
        {
            r.RegisterResolver(services);
        }
    }

    public static OttoSchemaConfig Empty(Type queryType, Type mutationType)
    {
        return new OttoSchemaConfig(
            queryType,
            mutationType,
            OttoScalarTypeMap.Default,
            ImmutableDictionary<Type, IGraphTypeBuilder>.Empty,
            new ScalarTypeMap()
            );
    }

    public OttoSchemaConfig UpdateLegacyBuilder<TType>(GraphTypeBuilder<TType> builder)
        where TType : class
    {
        return this with
        {
            LegacyBuilders = LegacyBuilders.SetItem(typeof(TType), builder)
        };
    }

    public QueryArguments GetGqlNetArguments(Type argsType, Dictionary<Type, IInputObjectGraphType> inputTypesCache)
    {
        if (argsType == null)
        {
            return null;
        }

        var builder = GetOrCreateBuilder(argsType);

        return builder.TypeConfig.ToGqlNetArguments(this, inputTypesCache);
    }

    public IGraphTypeBuilder GetOrCreateBuilder(Type modelType)
    {
        if (!LegacyBuilders.TryGetValue(modelType, out var untypedBuilder))
        {
            var builderType = typeof(GraphTypeBuilder<>).MakeGenericType(modelType);
            untypedBuilder = (IGraphTypeBuilder)Activator.CreateInstance(builderType, new object[] {LegacyScalars});
        }

        return untypedBuilder;
    }

    public OttoSchemaConfig AddScalarType(Type clrType, Type graphType)
    {
        var newScalars = Scalars.Add(clrType, graphType);

        return this with
        {
            Scalars = newScalars
        };
    }
}
