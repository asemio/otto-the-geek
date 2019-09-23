using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
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
            // TODO: timespan
        };
        private static readonly Dictionary<PropertyInfo, Type> NoResolvers = new Dictionary<PropertyInfo, Type>();
        private readonly Type _connectionResolver;
        private readonly Dictionary<PropertyInfo, FieldResolverConfiguration> _fieldResolvers;
        private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;

        public GraphTypeBuilder() : this(null, new Dictionary<PropertyInfo, FieldResolverConfiguration>(), new PropertyInfo[0])
        {

        }
        private GraphTypeBuilder(
            Type connectionResolver,
            Dictionary<PropertyInfo, FieldResolverConfiguration> scalarFieldResolvers,
            IEnumerable<PropertyInfo> propertiesToIgnore
            )
        {
            _connectionResolver = connectionResolver;
            _fieldResolvers = scalarFieldResolvers;
            _propertiesToIgnore = propertiesToIgnore;
        }

        internal GraphTypeBuilder<TModel> WithConnectionResolver<TResolver>() where TResolver : IConnectionResolver<TModel>
        {
            return Clone(connectionResolver: typeof(TResolver));
        }

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

        internal GraphTypeBuilder<TModel> WithResolverConfiguration(PropertyInfo prop, FieldResolverConfiguration config)
        {
            var dict = new Dictionary<PropertyInfo, FieldResolverConfiguration>(_fieldResolvers);
            dict[prop] = config;

            return Clone(fieldResolvers: dict);

        }

        public ObjectGraphType<TModel> BuildGraphType(GraphTypeCache cache, IServiceCollection services)
        {
            var graphType = new ObjectGraphType<TModel>
            {
                Name = GraphTypeName
            };
            if(!cache.TryPrime(graphType))
            {
                return cache.GetOrCreate<TModel>(services);
            }

            foreach(var prop in typeof(TModel).GetProperties().Except(_propertiesToIgnore))
            {
                if(CSharpToGraphqlTypeMapping.TryGetValue(prop.PropertyType, out var graphQlType))
                {
                    graphType.Field(
                        type: graphQlType,
                        name: prop.Name
                    );
                }
                else if(_fieldResolvers.TryGetValue(prop, out var resolverConfig))
                {
                    graphType.AddField(resolverConfig.ConfigureField(prop, cache, services));
                }
                else
                {
                    throw new UnableToResolveException(prop);
                }
            }
            return graphType;
        }
        private GraphTypeBuilder<TModel> Clone(
            Type connectionResolver = null,
            Dictionary<PropertyInfo, FieldResolverConfiguration> fieldResolvers = null,
            IEnumerable<PropertyInfo> propertiesToIgnore = null
            )
        {
            return new GraphTypeBuilder<TModel>(
                connectionResolver: connectionResolver ?? _connectionResolver,
                scalarFieldResolvers: fieldResolvers ?? _fieldResolvers,
                propertiesToIgnore: propertiesToIgnore ?? _propertiesToIgnore
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
    }
}
