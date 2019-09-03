using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GraphQL.Types;

namespace OttoTheGeek.Core
{
    public sealed class SchemaBuilder<TQuery>
    {
        private readonly Dictionary<Type, Type> _queryFieldResolvers;

        public SchemaBuilder()
        {
            _queryFieldResolvers = new Dictionary<Type, Type>();
        }

        private SchemaBuilder(Dictionary<Type, Type> resolvers)
        {

        }

        public QueryFieldConfigBuilder<TQuery, TProp> QueryField<TProp>(Expression<Func<TQuery, TProp>> expr)
        {
            var propInfo = expr.PropertyInfoForSimpleGet();
            return new QueryFieldConfigBuilder<TQuery, TProp>(this, propInfo);
        }

        internal SchemaBuilder<TQuery> WithQueryFieldResolver(Type iface, Type implementation)
        {
            var dict = new Dictionary<Type, Type>(_queryFieldResolvers);
            dict[iface] = implementation;

            return new SchemaBuilder<TQuery>(dict);
        }

        public OttoSchema Build()
        {
            return new OttoSchema();
        }
    }

    public sealed class OttoSchema
    {
        public IComplexGraphType QueryType { get; }
    }
}