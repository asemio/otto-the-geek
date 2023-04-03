using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;
using OttoTheGeek.Internal.Authorization;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.TypeModel;

public record OttoFieldConfig(
    PropertyInfo Property,
    Type ModelType,
    Nullability Nullability,
    Type ArgumentsType,
    FieldResolverConfiguration ResolverConfiguration,
    OrderByBuilder OrderByBuilder,
    AuthResolverStub AuthResolver
)
{
    public static OttoFieldConfig ForProperty(PropertyInfo prop, Type modelType)
    {
        return new OttoFieldConfig(prop, modelType, Nullability.Unspecified, null, null, null, new NullAuthResolverStub());
    }

    public OttoFieldConfig ConfigureOrderBy<TEntity>(Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator)
    {
        var builder = OrderByBuilder;
        if (builder == null)
        {
            builder = OrderByBuilder.FromPropertyInfo(Property);
        }

        return this with { OrderByBuilder = configurator((OrderByBuilder<TEntity>) builder) };
    }

    public FieldType ToGqlNetField(OttoSchemaConfig config,
        Dictionary<Type, IComplexGraphType> graphTypes, Dictionary<Type, IInputObjectGraphType> inputGraphTypes)
    {
        FieldType field;
        if (TryGetScalarGraphType (config.Scalars, out var graphQlType))
        {
            AuthResolver.ValidateGraphqlType(graphQlType, Property);
            field = new FieldType
            {
                Type = graphQlType,
                Name = Property.Name,
                Resolver = AuthResolver.GetResolver(NameFieldResolver.Instance)
            };
        }
        else if (Property.PropertyType.UnwrapNullable().IsEnum)
        {
            var enumGraphType = typeof(OttoEnumGraphType<>).MakeGenericType(Property.PropertyType.UnwrapNullable());
            enumGraphType = Property.PropertyType.IsNullable()
                ? enumGraphType
                : typeof(NonNullGraphType<>).MakeGenericType(enumGraphType);
            
            AuthResolver.ValidateGraphqlType(enumGraphType, Property);
            field = new FieldType
            {
                Type = enumGraphType,
                Name = Property.Name,
                Resolver = AuthResolver.GetResolver(NameFieldResolver.Instance),
            };
        }
        else
        {
            if(ResolverConfiguration == null)
            {
                throw new UnableToResolveException (Property, ModelType);
            }

            var graphType = GetResolvableGraphType(config, graphTypes);

            field = ResolverConfiguration.ConfigureField(Property, config, graphType, inputGraphTypes);
            field.Resolver = AuthResolver.GetResolver(field.Resolver);
            field.Arguments = config.GetGqlNetArguments(ArgumentsType, inputGraphTypes);
        }

        var descAttr = Property.GetCustomAttribute<DescriptionAttribute>();
        field.Description = descAttr?.Description;

        return field;
    }
    
    public FieldType ToGqlNetInputField(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> graphTypes)
    {
        var description = Property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var (gt, gtt) = GetGraphTypeConfiguration(config, graphTypes);

        return new FieldType
        {
            Type = gt,
            ResolvedType = gtt ?? (IGraphType)Activator.CreateInstance(gt),
            Name = Property.Name,
            Description = description,
        };
    }
    
    public bool TryGetScalarGraphType (OttoScalarTypeMap scalars, out Type type)
    {
        var t = Property.PropertyType;
        return TryGetScalarGraphType(t, scalars, out type);
    }

    private bool TryGetScalarGraphType(Type t, OttoScalarTypeMap scalars, out Type type)
    {
        if (!scalars.Map.TryGetValue(t, out type))
        {
            return false;
        }

        switch (Nullability)
        {
            case Nullability.Unspecified:
                return true;
            case Nullability.NonNull:
                type = type.MakeNonNullable();
                break;
            case Nullability.Nullable:
                type = type.UnwrapGqlNetNonNullable();
                break;
        }

        return true;
    }

    public QueryArgument ToGqlNetQueryArgument(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> inputTypes)
    {
        var desc = Property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var (gtt, gt) = GetGraphTypeConfiguration(config, inputTypes);

        if (gt == null)
        {
            return new QueryArgument(gtt)
            {
                Name = Property.Name,
                Description = desc,
            };
        }

        return new QueryArgument(gt)
        {
            Name = Property.Name,
            Description = desc,
        };
    }

    public (Type, IGraphType) GetGraphTypeConfiguration<TObjectGraphType>(
        OttoSchemaConfig config,
        IDictionary<Type, TObjectGraphType> cachedGraphTypes)
        where TObjectGraphType : IGraphType
    {
        var (graphTypeType, graphType) = GetCoreGraphTypeConfiguration(config, cachedGraphTypes);

        if (graphTypeType != null)
        {
            if (Property.PropertyType.IsEnumerable())
            {
                graphTypeType = typeof(ListGraphType<>).MakeGenericType(graphTypeType);

                return (graphTypeType, null);
            }

            var isClrScalar =
                typeof(ScalarGraphType).IsAssignableFrom(graphTypeType)
                || typeof(EnumerationGraphType).IsAssignableFrom(graphTypeType);

            var needsNonNull =
                (isClrScalar && !Property.PropertyType.IsNullable())
                || (Nullability != Nullability.Nullable);

            needsNonNull = needsNonNull && !graphTypeType.IsGenericFor(typeof(NonNullGraphType<>));

            if (needsNonNull)
            {
                return (typeof(NonNullGraphType<>).MakeGenericType(graphTypeType), null);
            }

            return (graphTypeType.UnwrapGqlNetNonNullable(), null);
        }

        if (Property.PropertyType.IsEnumerable())
        {
            if (ResolverConfiguration.ConnectionType == null)
            {
                return (null, new ListGraphType(graphType));
            }

            return (null, cachedGraphTypes[ResolverConfiguration.ConnectionType]);
        }

        if (Nullability != Nullability.Nullable)
        {
            return (null, new NonNullGraphType(graphType));
        }
        
        return (null, graphType);
    }

    private (Type, IGraphType) GetCoreGraphTypeConfiguration<TObjectGraphType>(
        OttoSchemaConfig config,
        IDictionary<Type, TObjectGraphType> cachedGraphTypes)
        where TObjectGraphType : IGraphType
    {
        var coreType = Property.PropertyType.UnwrapNullableAndEnumerable();

        if (config.Scalars.Map.TryGetValue(coreType, out var scalarGraphType))
        {
            return (scalarGraphType, null);
        }

        if (coreType.IsEnum)
        {
            return (typeof(OttoEnumGraphType<>).MakeGenericType(coreType), null);
        }

        if (typeof(OrderValue).IsAssignableFrom(Property.PropertyType))
        {
            var builder = OrderByBuilder ?? OrderByBuilder.FromPropertyInfo(Property);

            var enumGraphType = builder.BuildGraphType();
            return (null, enumGraphType);
        }

        return (null, cachedGraphTypes[coreType]);
    }

    public bool IsScalarLike(OttoSchemaConfig config)
    {
        var coreType = Property.PropertyType.UnwrapNullableAndEnumerable();
        if (config.Scalars.Map.ContainsKey(coreType))
        {
            return true;
        }

        if (coreType.IsEnum)
        {
            return true;
        }

        return false;
    }

    private IGraphType GetResolvableGraphType(OttoSchemaConfig config,
        Dictionary<Type, IComplexGraphType> graphTypes)
    {
        if (Property.PropertyType.GetEnumerableElementType() == ResolverConfiguration.CoreClrType)
        {
            if (config.Scalars.Map.TryGetValue(Property.PropertyType, out var scalarGraphType))
            {
                return (IGraphType)Activator.CreateInstance(scalarGraphType);
            }
        }
        
        if (!graphTypes.TryGetValue(ResolverConfiguration.CoreClrType, out var graphType))
        {
            throw new UnableToResolveException(Property, ModelType);
        }

        return graphType;
    }
}
