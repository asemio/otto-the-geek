﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using OttoTheGeek.Internal;

namespace OttoTheGeek.TypeModel;

[Flags]
public enum InputOutputKind
{
    Output = 1,
    Input = 2,
    Both = Input | Output,
}

public record OttoTypeConfig(
    string Name,
    Type ClrType,
    ImmutableHashSet<string> IgnoredProperties,
    ImmutableHashSet<Type> Interfaces,
    ImmutableDictionary<string, OttoFieldConfig> Fields
)
{
    private static ImmutableHashSet<string> EmptyProperties => ImmutableHashSet<string>.Empty;
    private static ImmutableHashSet<Type> EmptyTypes => ImmutableHashSet<Type>.Empty;
    
    private OttoTypeConfig(string name, Type clrType)
        : this(name, clrType, EmptyProperties, EmptyTypes, FieldsForType(clrType))
    {
        
    }

    public static OttoTypeConfig ForOutputType<T>()
    {
        return ForOutputType(typeof(T));
    }
    public static OttoTypeConfig ForOutputType(Type t)
    {
        return ForType(t);
    }

    private static OttoTypeConfig ForType(Type t)
    {
        return new OttoTypeConfig(DefaultName(t), t);
    }

    public OttoTypeConfig IgnoreProperty(PropertyInfo prop)
    {
        var newFields = Fields.Remove(prop.Name);
        var newIgnored = IgnoredProperties.Add(prop.Name);

        return this with
        {
            Fields = newFields,
            IgnoredProperties = newIgnored,
        };
    }

    public OttoTypeConfig ConfigureField(PropertyInfo prop, Func<OttoFieldConfig, OttoFieldConfig> configTransform)
    {
        var existing = GetFieldConfig(prop);

        return this with
        {
            Fields = Fields.SetItem(prop.Name, configTransform(existing))
        };
    }

    public OttoTypeConfig ConfigureField<T, TProp>(Expression<Func<T, TProp>> propExpr, Func<OttoFieldConfig, OttoFieldConfig> configTransform)
    {
        var prop = propExpr.PropertyInfoForSimpleGet();

        return ConfigureField(prop, configTransform);
    }

    public IComplexGraphType ToGqlNetGraphType(OttoSchemaConfig config)
    {
        var graphType = CreatGraphTypeStub();
        graphType.Name = Name;
        graphType.Description = ClrType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        return graphType;
    }
    
    public IInputObjectGraphType ToGqlNetInputGraphType(OttoSchemaConfig config)
    {
        var graphType = new InputObjectGraphType();
        graphType.Name = $"{Name}Input";
        graphType.Description = ClrType.GetCustomAttribute<DescriptionAttribute>()?.Description;

        return graphType;
    }

    public QueryArguments ToGqlNetArguments(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> inputTypes)
    {
        var args = Fields.Values
            .Where(x => !IgnoredProperties.Contains(x.Property.Name))
            .Select(x => x.ToGqlNetQueryArgument(config, inputTypes))
            .ToArray();

        return new QueryArguments(args);
    }

    public IEnumerable<PropertyInfo> GetRelevantProperties()
    {
        var props = ClrType
            .GetProperties()
            .Where(x => !IgnoredProperties.Contains(x.Name))
            .Where(x => x.CanRead && x.GetMethod?.IsStatic == false)
            .ToArray();

        return props;
    }

    public IEnumerable<OttoFieldConfig> GetRelevantFieldConfigs()
    {
        return Fields.Values
            .Where(x => !IgnoredProperties.Contains(x.Property.Name));
    }

    private IComplexGraphType CreatGraphTypeStub()
    {
        if (ClrType.IsInterface)
        {
            return new InterfaceGraphType();
        }

        return new ObjectGraphType();
    }
    
    private OttoFieldConfig GetFieldConfig(PropertyInfo prop)
    {
        var existing = Fields.GetValueOrDefault(prop.Name, OttoFieldConfig.ForProperty(prop, ClrType));
        return existing;
    }

    private static ImmutableDictionary<string, OttoFieldConfig> FieldsForType(Type t)
    {
        return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToImmutableDictionary(x => x.Name, x => OttoFieldConfig.ForProperty(x, t));
    }

    private static string DefaultName(Type clrType)
    {
        if (IsConnection(clrType))
        {
            return $"{GetConnectionElemType(clrType).Name}Connection";
        }

        return SanitizedTypeName(clrType);
    }

    private static bool IsConnection(Type clrType) => clrType.IsConstructedGenericType &&
        clrType.GetGenericTypeDefinition () == typeof (Connections.Connection<>);

    private static Type GetConnectionElemType (Type clrType) => clrType.GetGenericArguments().Single();

    private static string SanitizedTypeName(Type clrType)
    {
        var typeName = clrType.Name;

        if(!clrType.IsGenericType)
        {
            return typeName;
        }

        var closedType = clrType.GetGenericArguments()[0];

        var trimmedName = typeName.Substring(0, typeName.IndexOf('`'));

        return $"{trimmedName}Of{closedType.Name}";
    }
}
