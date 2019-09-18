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
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new ListQueryFieldBuilder<TQuery, TElem>(this, propInfo);
        }

        public QueryFieldBuilder<TQuery, TProp> QueryField<TProp>(Expression<Func<TQuery, TProp>> expr)
            where TProp : class
        {
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new QueryFieldBuilder<TQuery, TProp>(this, propInfo);
        }

        public ConnectionFieldBuilder<TQuery, TProp> ConnectionField<TProp>(Expression<Func<TQuery, IEnumerable<TProp>>> expr)
            where TProp : class
        {
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new ConnectionFieldBuilder<TQuery, TProp>(this, propInfo);
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

        internal SchemaBuilder<TQuery> WithGraphTypeBuilder<TType>(GraphTypeBuilder<TType> builder)
            where TType : class
        {
            var dict = new Dictionary<Type, IGraphTypeBuilder>(_builders);
            dict[typeof(TType)] = builder;

            return new SchemaBuilder<TQuery>(dict, _connectionProperties);
        }

        public OttoSchema Build(IServiceCollection services)
        {
            var graphTypeCache = new GraphTypeCache(_builders);
            var queryType = new ObjectGraphType
            {
                Name = "Query"
            };
            foreach(var prop in typeof(TQuery).GetProperties())
            {
                if(_builders.TryGetValue(prop.PropertyType, out var builder))
                {
                    builder.ConfigureScalarQueryField(prop, queryType, services, graphTypeCache);
                    continue;
                }

                var elemType = prop.PropertyType.GetEnumerableElementType();

                if(elemType != null && _builders.TryGetValue(elemType, out var listElemBuilder))
                {
                    if(_connectionProperties.Contains(prop))
                    {
                        listElemBuilder.ConfigureConnectionField(prop, queryType, services, graphTypeCache);
                    }
                    else
                    {
                        listElemBuilder.ConfigureListQueryField(prop, queryType, services, graphTypeCache);
                    }
                    continue;
                }

                throw new UnableToResolveException(prop);
            }
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