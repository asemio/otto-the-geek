using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Connections;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class GraphTypeBuilder<TModel> : IGraphTypeBuilder
        where TModel : class
    {
        private static readonly IReadOnlyDictionary<Type, Type> CSharpToGraphqlTypeMapping = new Dictionary<Type, Type>{
            [typeof(string)]            = typeof(NonNullGraphType<StringGraphType>),
            [typeof(int)]               = typeof(NonNullGraphType<IntGraphType>),
            [typeof(long)]              = typeof(NonNullGraphType<IntGraphType>),
            [typeof(double)]            = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(float)]             = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(decimal)]           = typeof(NonNullGraphType<DecimalGraphType>),
            [typeof(bool)]              = typeof(NonNullGraphType<BooleanGraphType>),
            [typeof(DateTime)]          = typeof(NonNullGraphType<DateGraphType>),
            [typeof(DateTimeOffset)]    = typeof(NonNullGraphType<DateTimeOffsetGraphType>),
            [typeof(Guid)]              = typeof(NonNullGraphType<IdGraphType>),
            [typeof(short)]             = typeof(NonNullGraphType<ShortGraphType>),
            [typeof(ushort)]            = typeof(NonNullGraphType<UShortGraphType>),
            [typeof(ulong)]             = typeof(NonNullGraphType<ULongGraphType>),
            [typeof(uint)]              = typeof(NonNullGraphType<UIntGraphType>),
            [typeof(TimeSpan)]          = typeof(NonNullGraphType<TimeSpanGraphType>),

            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(int?)]              = typeof(IntGraphType),
            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(double?)]           = typeof(FloatGraphType),
            [typeof(float?)]            = typeof(FloatGraphType),
            [typeof(decimal?)]          = typeof(DecimalGraphType),
            [typeof(bool?)]             = typeof(BooleanGraphType),
            [typeof(DateTime?)]         = typeof(DateGraphType),
            [typeof(DateTimeOffset?)]   = typeof(DateTimeOffsetGraphType),
            [typeof(Guid?)]             = typeof(IdGraphType),
            [typeof(short?)]            = typeof(ShortGraphType),
            [typeof(ushort?)]           = typeof(UShortGraphType),
            [typeof(ulong?)]            = typeof(ULongGraphType),
            [typeof(uint?)]             = typeof(UIntGraphType),
            [typeof(TimeSpan?)]         = typeof(TimeSpanGraphType),
        };
        private readonly Dictionary<PropertyInfo, FieldResolverConfiguration> _fieldResolvers;
        private readonly Dictionary<PropertyInfo, Nullability> _nullabilityOverrides;
        private readonly Dictionary<PropertyInfo, OrderByBuilder> _orderByBuilders;
        private readonly Dictionary<PropertyInfo, Type> _graphTypeOverrides;
        private enum Nullability { NonNull, Nullable }
        private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;
        private readonly IEnumerable<Type> _interfaces;

        public GraphTypeBuilder() : this(
            new Dictionary<PropertyInfo, FieldResolverConfiguration>(),
            new Dictionary<PropertyInfo, Nullability>(),
            new Dictionary<PropertyInfo, OrderByBuilder>(),
            new Dictionary<PropertyInfo, Type>(),
            new PropertyInfo[0],
            new Type[0])
        {

        }
        private GraphTypeBuilder(
            Dictionary<PropertyInfo, FieldResolverConfiguration> scalarFieldResolvers,
            Dictionary<PropertyInfo, Nullability> nullabilityOverrides,
            Dictionary<PropertyInfo, OrderByBuilder> orderByBuilders,
            Dictionary<PropertyInfo, Type> graphTypeOverrides,
            IEnumerable<PropertyInfo> propertiesToIgnore,
            IEnumerable<Type> interfaces
            )
        {
            _fieldResolvers = scalarFieldResolvers;
            _propertiesToIgnore = propertiesToIgnore;
            _nullabilityOverrides = nullabilityOverrides;
            _orderByBuilders = orderByBuilders;
            _graphTypeOverrides = graphTypeOverrides;
            _interfaces = interfaces;
        }

        public bool NeedsRegistration => _interfaces.Any();

        public ScalarFieldBuilder<TModel, TProp> ScalarField<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ScalarFieldBuilder<TModel, TProp>(this, prop);
        }

        public LooseScalarFieldBuilder<TModel, TProp> LooseScalarField<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new LooseScalarFieldBuilder<TModel, TProp>(this, prop);
        }

        public ListFieldBuilder<TModel, TProp> ListField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();

            return new ListFieldBuilder<TModel, TProp>(this, prop);
        }

        public LooseListFieldBuilder<TModel, TProp> LooseListField<TProp>(Expression<Func<TModel, IEnumerable<TProp>>> propertyExpression)
        {
            return new LooseListFieldBuilder<TModel, TProp>(this, propertyExpression);
        }

        public GraphTypeBuilder<TModel> IgnoreProperty<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var props = _propertiesToIgnore.Concat(new[] { prop }).ToArray();

            return Clone(propertiesToIgnore: props);
        }

        public GraphTypeBuilder<TModel> NonNullable<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var dict = new Dictionary<PropertyInfo, Nullability>(_nullabilityOverrides);
            dict[prop] = Nullability.NonNull;

            return Clone(nullabilityOverrides: dict);
        }

        public GraphTypeBuilder<TModel> Nullable<TProp>(Expression<Func<TModel, TProp>> propertyExpression)
        {
            var prop = propertyExpression.PropertyInfoForSimpleGet();
            var dict = new Dictionary<PropertyInfo, Nullability>(_nullabilityOverrides);
            dict[prop] = Nullability.Nullable;

            return Clone(nullabilityOverrides: dict);
        }

        public GraphTypeBuilder<TModel> ConfigureOrderBy<TEntity>(
            Expression<Func<TModel, OrderValue<TEntity>>> propSelector, Func<OrderByBuilder<TEntity>, OrderByBuilder<TEntity>> configurator
            )
        {
            var prop = propSelector.PropertyInfoForSimpleGet();
            var dict = new Dictionary<PropertyInfo, OrderByBuilder>(_orderByBuilders);
            dict[prop] = configurator(GetOrderByBuilder<TEntity>(prop));

            return Clone(orderByBuilders: dict);
        }

        public GraphTypeBuilder<TModel> Interface<TInterface>()
        {
            var tIface = typeof(TInterface);
            var tModel = typeof(TModel);

            if(!tIface.IsAssignableFrom(tModel))
            {
                throw new ArgumentException($"{tModel.Name} does not implement {tIface.Name}");
            }

            var interfaces = _interfaces.Concat(new[] {tIface}).Distinct().ToArray();

            return Clone(interfaces: interfaces);
        }

        internal GraphTypeBuilder<TModel> WithResolverConfiguration(PropertyInfo prop, FieldResolverConfiguration config)
        {
            var dict = new Dictionary<PropertyInfo, FieldResolverConfiguration>(_fieldResolvers);
            dict[prop] = config;

            return Clone(fieldResolvers: dict);
        }

        internal GraphTypeBuilder<TModel> WithGraphTypeOverride(PropertyInfo prop, Type graphType)
        {
            var dict = new Dictionary<PropertyInfo, Type>(_graphTypeOverrides);
            dict[prop] = graphType;

            return Clone(graphTypeOverrides: dict);
        }

        IComplexGraphType IGraphTypeBuilder.BuildGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            return BuildGraphType(cache, services);
        }
        public ComplexGraphType<TModel> BuildGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            var graphType = CreateGraphTypeCore(cache, services);
            graphType.Name = GraphTypeName;

            if(!cache.TryPrime(graphType))
            {
                return cache.GetOrCreate<TModel>(services);
            }

            foreach(var prop in typeof(TModel).GetProperties().Except(_propertiesToIgnore))
            {
                if(TryGetScalarGraphType(prop, out var graphQlType))
                {
                    graphType.Field(
                        type: graphQlType,
                        name: prop.Name
                    );
                }
                else if(_fieldResolvers.TryGetValue(prop, out var resolverConfig))
                {
                    graphType.AddField(fieldType: resolverConfig.ConfigureField(prop, cache, services));
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
            return graphType;
        }

        public QueryArguments BuildQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            var args = typeof(TModel)
                .GetProperties()
                .Except(_propertiesToIgnore)
                .Select(prop => ToQueryArgument(prop));

            return new QueryArguments(args);
        }

        private GraphTypeBuilder<TModel> Clone(
            Dictionary<PropertyInfo, FieldResolverConfiguration> fieldResolvers = null,
            Dictionary<PropertyInfo, Nullability> nullabilityOverrides = null,
            Dictionary<PropertyInfo, OrderByBuilder> orderByBuilders = null,
            Dictionary<PropertyInfo, Type> graphTypeOverrides = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null,
            IEnumerable<Type> interfaces = null
            )
        {
            return new GraphTypeBuilder<TModel>(
                scalarFieldResolvers: fieldResolvers ?? _fieldResolvers,
                nullabilityOverrides: nullabilityOverrides ?? _nullabilityOverrides,
                orderByBuilders: orderByBuilders ?? _orderByBuilders,
                propertiesToIgnore: propertiesToIgnore ?? _propertiesToIgnore,
                graphTypeOverrides: graphTypeOverrides ?? _graphTypeOverrides,
                interfaces: interfaces ?? _interfaces
            );
        }

        private string GraphTypeName =>
            IsConnection
            ? $"{GetConnectionElemType().Name}Connection"
            : typeof(TModel).Name;

        private bool IsConnection => typeof(TModel).IsConstructedGenericType
            && typeof(TModel).GetGenericTypeDefinition() == typeof(Connection<>);

        private Type GetConnectionElemType()
        {
            return typeof(TModel).GetGenericArguments().Single();
        }

        private QueryArgument ToQueryArgument(PropertyInfo prop)
        {
            if(TryGetScalarGraphType(prop, out var graphType))
            {
                return new QueryArgument(graphType)
                {
                    Name = prop.Name
                };
            }

            if(typeof(OrderValue).IsAssignableFrom(prop.PropertyType))
            {
                _orderByBuilders.TryGetValue(prop, out var builder);
                builder = builder ?? OrderByBuilder.FromPropertyInfo(prop);
                var enumGraphType = builder.BuildGraphType();
                if(_nullabilityOverrides.TryGetValue(prop, out var nullability) && nullability == Nullability.NonNull)
                {
                    enumGraphType = new NonNullGraphType(enumGraphType);
                }
                return new QueryArgument(enumGraphType)
                {
                    Name = prop.Name
                };
            }

            throw new UnableToResolveException(prop);
        }

        private bool TryGetScalarGraphType(PropertyInfo prop, out Type type)
        {
            type = null;

            if(_graphTypeOverrides.TryGetValue(prop, out type))
            {
                return true;
            }

            if(!CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphType))
            {
                return false;
            }

            type = graphType;

            if(!_nullabilityOverrides.TryGetValue(prop, out var nullability))
            {
                return true;
            }

            if(nullability == Nullability.NonNull)
            {
                type = graphType.MakeNonNullable();
            }
            else if(nullability == Nullability.Nullable)
            {
                type = graphType.UnwrapNonNullable();
            }
            return true;
        }

        private OrderByBuilder<TEntity> GetOrderByBuilder<TEntity>(PropertyInfo prop)
        {
            _orderByBuilders.TryGetValue(prop, out var builder);

            return ((OrderByBuilder<TEntity>)builder) ?? new OrderByBuilder<TEntity>();
        }

        private ComplexGraphType<TModel> CreateGraphTypeCore(GraphTypeCache cache, IServiceCollection services)
        {
            if(typeof(TModel).IsInterface)
            {
                return new InterfaceGraphType<TModel>();
            }

            var objectGraphType = new ObjectGraphType<TModel>();
            foreach(var iFace in _interfaces)
            {
                objectGraphType.AddResolvedInterface((IInterfaceGraphType)cache.GetOrCreate(iFace, services));
            }

            return objectGraphType;
        }
    }
}
