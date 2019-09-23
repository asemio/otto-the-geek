using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class SchemaBuilder<TQuery>
        where TQuery : class
    {
        private readonly Dictionary<Type, IGraphTypeBuilder> _builders;
        private readonly IEnumerable<PropertyInfo> _connectionProperties;

        public SchemaBuilder() : this(new Dictionary<Type, IGraphTypeBuilder>(), new PropertyInfo[0])
        {
        }
        private SchemaBuilder(Dictionary<Type, IGraphTypeBuilder> builders, IEnumerable<PropertyInfo> connectionProperties)
        {
            _builders = builders;
            _connectionProperties = connectionProperties;
        }

        public ListQueryFieldBuilder<TQuery, TElem> ListQueryField<TElem>(Expression<Func<TQuery, IEnumerable<TElem>>> expr)
            where TElem : class
        {
            return new ListQueryFieldBuilder<TQuery, TElem>(this, expr);
        }

        public QueryFieldBuilder<TQuery, TProp> QueryField<TProp>(Expression<Func<TQuery, TProp>> expr)
            where TProp : class
        {
            return new QueryFieldBuilder<TQuery, TProp>(this, expr);
        }

        public ConnectionFieldBuilder<TQuery, TProp> ConnectionField<TProp>(Expression<Func<TQuery, IEnumerable<TProp>>> expr)
            where TProp : class
        {
            return new ConnectionFieldBuilder<TQuery, TProp>(this, expr);
        }

        internal SchemaBuilder<TQuery> ConnectionProperty(PropertyInfo prop)
        {
            return new SchemaBuilder<TQuery>(_builders, _connectionProperties.Concat(new[] { prop }).ToArray());
        }

        public SchemaBuilder<TQuery> GraphType<TType>(Func<GraphTypeBuilder<TType>, GraphTypeBuilder<TType>> configurator)
            where TType : class
        {
            var dict = new Dictionary<Type, IGraphTypeBuilder>(_builders);
            dict[typeof(TType)] = configurator(GetGraphTypeBuilder<TType>());
            return new SchemaBuilder<TQuery>(dict, _connectionProperties);
        }

        public OttoSchema Build(IServiceCollection services)
        {
            var graphTypeCache = new GraphTypeCache(_builders);
            var queryType = graphTypeCache.GetOrCreate<TQuery>(services);

            return new OttoSchema(queryType);
        }

        private GraphTypeBuilder<TType> GetGraphTypeBuilder<TType>()
            where TType : class
        {
            _builders.TryGetValue(typeof(TType), out var untypedBuilder);
            var builder =
                ((GraphTypeBuilder<TType>)untypedBuilder)
                ?? new GraphTypeBuilder<TType>();

            return builder;
        }
    }
}