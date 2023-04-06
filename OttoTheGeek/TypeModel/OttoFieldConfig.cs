using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;
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
        var desc = Property.GetCustomAttribute<DescriptionAttribute>()?.Description;
        var (gtt, gt) = GetGraphTypeConfiguration(config, graphTypes);
        if (gtt != null)
        {
            AuthResolver.ValidateGraphqlType(gtt, Property);
            if (gtt.IsGenericFor(typeof(ListGraphType<>)))
            {
                if(ResolverConfiguration == null)
                {
                    throw new UnableToResolveException (Property, ModelType);
                }
            }
            return new FieldType
            {
                Type = gtt,
                Name = Property.Name,
                Description = desc,
                Resolver = AuthResolver.GetResolver(ResolverConfiguration?.CreateGraphQLResolver() ?? NameFieldResolver.Instance)
            };
        }
        
        if(ResolverConfiguration == null)
        {
            throw new UnableToResolveException (Property, ModelType);
        }

        AuthResolver.ValidateGraphqlType(gt, Property);
        var field = new FieldType
        {
            Name = Property.Name,
            Description = desc,
            Arguments = config.GetGqlNetArguments(ArgumentsType, inputGraphTypes),
            ResolvedType = gt
        };

        field.Resolver = AuthResolver.GetResolver(ResolverConfiguration.CreateGraphQLResolver());

        return field;
    }
    
    public FieldType ToGqlNetInputField(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> graphTypes)
    {
        var description = Property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var (gt, gtt) = GetGraphTypeConfiguration(config, graphTypes, isInputType: true);

        return new FieldType
        {
            Type = gt,
            ResolvedType = gtt,
            Name = Property.Name,
            Description = description,
        };
    }

    public QueryArgument ToGqlNetQueryArgument(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> inputTypes)
    {
        var desc = Property.GetCustomAttribute<DescriptionAttribute>()?.Description;

        var (gtt, gt) = GetGraphTypeConfiguration(config, inputTypes, isInputType: true);

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
        IDictionary<Type, TObjectGraphType> cachedGraphTypes,
        bool isInputType = false)
        where TObjectGraphType : IGraphType
    {
        var (graphTypeType, graphType) = GetCoreGraphTypeConfiguration(config, cachedGraphTypes);

        if (graphTypeType != null)
        {
            if (Property.PropertyType.IsEnumerable())
            {
                graphTypeType = typeof(ListGraphType<>).MakeGenericType(graphTypeType.MakeNonNullable());

                return (graphTypeType, null);
            }

            var treatAsScalar =
                typeof(ScalarGraphType).IsAssignableFrom(graphTypeType)
                || typeof(EnumerationGraphType).IsAssignableFrom(graphTypeType)
                ;

            var defaultNullability = treatAsScalar ? Nullability.NonNull : Nullability.Nullable;

            var computedNullability =
                Nullability == Nullability.Unspecified ?
                    (
                        Property.PropertyType.IsNullable()
                            ? Nullability.Nullable
                            : defaultNullability
                    )
                    : Nullability;
            
            if(computedNullability == Nullability.NonNull)
            {
                return (typeof(NonNullGraphType<>).MakeGenericType(graphTypeType), null);
            }

            return (graphTypeType, null);
        }

        if (Property.PropertyType.IsEnumerable())
        {
            if (ResolverConfiguration?.ConnectionType == null)
            {
                if (isInputType)
                {
                    return (null, new ListGraphType(new NonNullGraphType(graphType)));
                }
                
                return (null, new ListGraphType(graphType));
            }

            return (null, cachedGraphTypes[ResolverConfiguration.ConnectionType]);
        }

        if (Nullability == Nullability.NonNull || isInputType && !(graphType is EnumerationGraphType))
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

        if (!cachedGraphTypes.TryGetValue(coreType, out var graphType))
        {
            var builder = config.GetOrCreateBuilder(coreType);

            if (typeof(TObjectGraphType) == typeof(IInputObjectGraphType))
            {
                graphType = (TObjectGraphType)builder.TypeConfig.ToGqlNetInputGraphType(config);
            }
            else
            {
                graphType = (TObjectGraphType)builder.TypeConfig.ToGqlNetGraphType(config);
            }
        }

        return (null, graphType);
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
}
