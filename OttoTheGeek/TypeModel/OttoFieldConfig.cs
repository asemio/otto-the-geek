using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using GraphQL;
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
        else
        {
            if(ResolverConfiguration == null)
            {
                throw new UnableToResolveException (Property, ModelType);
            }
            
            field = ResolverConfiguration.ConfigureField(Property, config, graphTypes[ResolverConfiguration.CoreClrType], inputGraphTypes);
            field.Resolver = AuthResolver.GetResolver(field.Resolver);
        }

        var descAttr = Property.GetCustomAttribute<DescriptionAttribute>();
        field.Description = descAttr?.Description;

        return field;
    }
    
    public FieldType ToGqlNetInputField(OttoSchemaConfig config, Dictionary<Type, IInputObjectGraphType> graphTypes)
    {
        FieldType field;
        if (TryGetScalarGraphType (config.Scalars, out var graphQlType))
        {
            field = new FieldType
            {
                Type = graphQlType,
                Name = Property.Name,
            };
        }
        else
        {
            var inputType = TryWrapNonNull(graphTypes[Property.PropertyType]);

            field = new FieldType
            {
                ResolvedType = inputType,
                Type = Property.PropertyType,
                Name = Property.Name
            };
        }
        
        var descAttr = Property.GetCustomAttribute<DescriptionAttribute>();
        field.Description = descAttr?.Description;

        return field;
    }
    
    public bool TryGetScalarGraphType (OttoScalarTypeMap scalars, out Type type)
    {
        if (!scalars.Map.TryGetValue(Property.PropertyType, out type))
        {
            return false;
        }

        switch (Nullability)
        {
            case Nullability.Unspecified:
                return true;
            case Nullability.NonNull:
                type = type.MakeNonNullable ();
                break;
            case Nullability.Nullable:
                type = type.UnwrapNonNullable ();
                break;
        }

        return true;
    }

    public IGraphType TryWrapNonNull(IGraphType inputType)
    {
        if (Nullability == Nullability.Nullable)
            return inputType;

        return new NonNullGraphType(inputType);
    }
}
