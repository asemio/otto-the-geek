using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using OttoTheGeek.Connections;

namespace OttoTheGeek.Internal
{
    public sealed class ConnectionFieldBuilder<T, TElem>
        where TElem : class
        where T : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly Expression<Func<T, IEnumerable<TElem>>> _propExpr;

        internal ConnectionFieldBuilder(SchemaBuilder<T> parent, Expression<Func<T, IEnumerable<TElem>>> propExpr)
        {
            _parent = parent;
            _propExpr = propExpr;
        }
        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : class, IConnectionResolver<TElem>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parent.GraphType<T>(
                b => b.WithResolverConfiguration(prop, new ConnectionResolverConfiguration<TElem, TResolver>())
                )
                .GraphType<Connection<TElem>>(
                    b => b
                        .ListField(x => x.Records)
                        .Preloaded()
                );
        }
    }
}