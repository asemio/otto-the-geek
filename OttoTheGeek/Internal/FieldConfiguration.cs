using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal.Authorization;

namespace OttoTheGeek.Internal
{
    internal sealed class FieldConfiguration<TModel>
    {
        public PropertyInfo Property { get; }
        public Nullability Nullability { get; private set; }
        public FieldResolverConfiguration ResolverConfiguration { get; private set; }
        public Type OverriddenGraphType { get; private set; }
        public AuthResolverStub AuthResolver { get; private set; } = new NullAuthResolverStub();
        public OrderByBuilder OrderByBuilder { get; private set; }

        public FieldConfiguration(PropertyInfo prop)
        {
            Property = prop;
        }
        public FieldConfiguration<TModel> WithNullable(Nullability nullability) =>
            With(x => x.Nullability, nullability);

        public FieldConfiguration<TModel> OverrideGraphType(Type t) =>
            With(x => x.OverriddenGraphType, t);

        public FieldConfiguration<TModel> WithResolverConfiguration(FieldResolverConfiguration r) =>
            With(x => x.ResolverConfiguration, r);

        public FieldConfiguration<TModel> WithAuthorization<TAuth>(Func<TAuth, bool> authCallback)
            where TAuth : class
            => With(x => x.AuthResolver, new AuthResolverStub<TAuth>(authCallback));

        public FieldConfiguration<TModel> ConfigureOrderBy<TEntity>(Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator)
        {
            return this.With(x => x.OrderByBuilder, configurator(GetOrderByBuilder<TEntity>()));
        }

        public FieldConfiguration<TModel> With<TProp>(Expression<Func<FieldConfiguration<TModel>, TProp>> propExpr, TProp newValue)
        {
            var newConfig = MemberwiseClone();

            var propToSet = propExpr.PropertyInfoForSimpleGet();

            propToSet.SetValue(newConfig, newValue);

            return (FieldConfiguration<TModel>)newConfig;
        }

        public IGraphType TryWrapNonNull(IGraphType inputType)
        {
            if (Nullability == Nullability.Nullable)
                return inputType;

            return new NonNullGraphType(inputType);
        }

        public void ConfigureInputTypeField(InputObjectGraphType<TModel> graphType, GraphTypeCache cache)
        {
            if (TryGetScalarGraphType (out var graphQlType))
            {
                graphType.Field (
                    type: graphQlType,
                    name: Property.Name
                );
            }
            else
            {
                var inputType = cache.GetOrCreateInputType(Property.PropertyType);
                inputType = TryWrapNonNull(inputType);

                graphType.AddField(new FieldType
                {
                    ResolvedType = inputType,
                    Type = Property.PropertyType,
                    Name = Property.Name
                });
            }
        }

        public bool TryGetScalarGraphType (out Type type)
        {
            type = OverriddenGraphType;

            if (type != null) {
                return true;
            }

            if (!ScalarTypeMap.TryGetGraphType (Property.PropertyType, out type))
            {
                return false;
            }

            if (Nullability == Nullability.Unspecified) {
                return true;
            }

            if (Nullability == Nullability.NonNull) {
                type = type.MakeNonNullable ();
            } else if (Nullability == Nullability.Nullable) {
                type = type.UnwrapNonNullable ();
            }
            return true;
        }

        public void ConfigureField(ComplexGraphType<TModel> graphType, GraphTypeCache cache, IServiceCollection services)
        {
            if (TryGetScalarGraphType (out var graphQlType))
            {
                AuthResolver.ValidateGraphqlType(graphQlType, Property);
                var field = new FieldType
                {
                    Type = graphQlType,
                    Name = Property.Name,
                    Resolver = AuthResolver.GetResolver(services, new BorrowedNameFieldResolver())
                };
                graphType.AddField (field);
            }
            else
            {
                if(ResolverConfiguration == null)
                {
                    throw new UnableToResolveException (Property);
                }

                var field = ResolverConfiguration.ConfigureField (Property, cache, services);
                field.Resolver = AuthResolver.GetResolver(services, field.Resolver);
                graphType.AddField (fieldType: field);
            }
        }

        private OrderByBuilder<TEntity> GetOrderByBuilder<TEntity>()
        {
            return (OrderByBuilder<TEntity>)OrderByBuilder ?? new OrderByBuilder<TEntity>();
        }
    }

}