using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
{
    public sealed class SchemaBuilder<TQuery>
    {
        private readonly Dictionary<Type, IGraphTypeBuilder> _builders;

        public SchemaBuilder() : this(new Dictionary<Type, IGraphTypeBuilder>())
        {
        }
        private SchemaBuilder(Dictionary<Type, IGraphTypeBuilder> builders)
        {
            _builders = builders;
        }

        public ListQueryFieldBuilder<TQuery, TElem> ListQueryField<TElem>(Expression<Func<TQuery, IEnumerable<TElem>>> expr)
            where TElem : class
        {
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new ListQueryFieldBuilder<TQuery, TElem>(this, propInfo, GetGraphTypeBuilder<TElem>());
        }

        public QueryFieldBuilder<TQuery, TProp> QueryField<TProp>(Expression<Func<TQuery, TProp>> expr)
            where TProp : class
        {
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new QueryFieldBuilder<TQuery, TProp>(this, propInfo, GetGraphTypeBuilder<TProp>());
        }

        public SchemaBuilder<TQuery> GraphType<TType>(Func<GraphTypeBuilder<TType>, GraphTypeBuilder<TType>> configurator)
            where TType : class
        {
            var dict = new Dictionary<Type, IGraphTypeBuilder>(_builders);
            dict[typeof(TType)] = configurator(GetGraphTypeBuilder<TType>());
            return new SchemaBuilder<TQuery>(dict);
        }

        internal SchemaBuilder<TQuery> WithGraphTypeBuilder<TType>(GraphTypeBuilder<TType> builder)
            where TType : class
        {
            var dict = new Dictionary<Type, IGraphTypeBuilder>(_builders);
            dict[typeof(TType)] = builder;

            return new SchemaBuilder<TQuery>(dict);
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
                    listElemBuilder.ConfigureListQueryField(prop, queryType, services, graphTypeCache);
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