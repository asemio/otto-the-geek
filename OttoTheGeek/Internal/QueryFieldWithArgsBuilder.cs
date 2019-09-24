using System;
using System.Linq.Expressions;

namespace OttoTheGeek.Internal
{
    public sealed class QueryFieldWithArgsBuilder<T, TProp, TArgs>
        where TProp : class
        where T : class
    {
        internal SchemaBuilder<T> Parent { get; }
        internal Expression<Func<T, TProp>> PropExpr { get; }

        internal QueryFieldWithArgsBuilder(SchemaBuilder<T> parent, Expression<Func<T, TProp>> propExpr)
        {
            Parent = parent;
            PropExpr = propExpr;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
        where TResolver : class, IScalarFieldWithArgsResolver<TProp, TArgs>
        {
            return Parent.GraphType<T>(
                b => b.LooseScalarField(PropExpr)
                    .WithArgs<TArgs>()
                    .ResolvesVia<TResolver>()
            );
        }
    }
}