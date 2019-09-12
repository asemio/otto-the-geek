using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Core
{
    public sealed class SchemaBuilder<TQuery>
    {
        private readonly Dictionary<Type, GraphTypeBuilder> _builders;

        public SchemaBuilder() : this(new Dictionary<Type, GraphTypeBuilder>())
        {
        }
        private SchemaBuilder(Dictionary<Type, GraphTypeBuilder> builders)
        {

            _builders = builders;
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

        internal SchemaBuilder<TQuery> WithGraphTypeBuilder<TType>(GraphTypeBuilder<TType> builder)
            where TType : class
        {
            var dict = new Dictionary<Type, GraphTypeBuilder>(_builders);
            dict[typeof(TType)] = builder;

            return new SchemaBuilder<TQuery>(dict);
        }

        public OttoSchema Build(IServiceCollection services)
        {
            var queryType = new ObjectGraphType
            {
                Name = "Query"
            };
            foreach(var prop in typeof(TQuery).GetProperties())
            {
                if(_builders.TryGetValue(prop.PropertyType, out var builder))
                {
                    builder.ConfigureScalarQueryField(prop, queryType, services);
                    continue;
                }

                var elemType = prop.PropertyType.GetEnumerableElementType();

                if(elemType != null && _builders.TryGetValue(elemType, out var listElemBuilder))
                {
                    listElemBuilder.ConfigureListQueryField(prop, queryType, services);
                    continue;
                }

                throw new UnableToResolveException(prop);
            }
            return new OttoSchema(queryType);
        }
    }

    public sealed class OttoSchema
    {
        public OttoSchema(IObjectGraphType queryType)
        {
            QueryType = queryType;
        }
        public IObjectGraphType QueryType { get; }
    }

    public sealed class QueryFieldGraphqlResolverProxy<T> : GraphQL.Resolvers.IFieldResolver<Task<T>>
    {
        private readonly IQueryFieldResolver<T> _resolver;

        public QueryFieldGraphqlResolverProxy(IQueryFieldResolver<T> resolver)
        {
            _resolver = resolver;
        }
        public Task<T> Resolve(ResolveFieldContext context)
        {
            return _resolver.Resolve();
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}