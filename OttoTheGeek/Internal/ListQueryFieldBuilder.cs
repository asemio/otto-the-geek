using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OttoTheGeek.Internal
{
    public sealed class ListQueryFieldBuilder<T, TElem>
        where TElem : class
        where T : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly Expression<Func<T, IEnumerable<TElem>>> _propExpr;

        internal ListQueryFieldBuilder(SchemaBuilder<T> parent, Expression<Func<T, IEnumerable<TElem>>> propExpr)
        {
            _parent = parent;
            _propExpr = propExpr;
        }
        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldResolver<TElem>
        {
            return _parent.GraphType<T>(
                b => b.LooseListField(_propExpr).ResolvesVia<TResolver>()
                );
        }

        public ListFieldWithArgsBuilder<T, TElem, TArgs> WithArgs<TArgs>()
        {
            return new ListFieldWithArgsBuilder<T, TElem, TArgs>(_parent, _propExpr);
        }

    }

    public sealed class ListFieldWithArgsBuilder<T, TElem, TArgs>
        where TElem : class
        where T : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly Expression<Func<T, IEnumerable<TElem>>> _propExpr;

        internal ListFieldWithArgsBuilder(SchemaBuilder<T> parent, Expression<Func<T, IEnumerable<TElem>>> propExpr)
        {
            _parent = parent;
            _propExpr = propExpr;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldWithArgsResolver<TElem, TArgs>
        {
            return _parent.GraphType<T>(
                b => b.LooseListField(_propExpr)
                    .WithArgs<TArgs>()
                    .ResolvesVia<TResolver>()
                );
        }
    }
}