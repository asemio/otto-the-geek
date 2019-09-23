using System;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class QueryFieldBuilder<T, TProp>
        where TProp : class
        where T : class
    {
        internal SchemaBuilder<T> Parent { get; }
        internal Expression<Func<T, TProp>> PropExpr { get; }

        internal QueryFieldBuilder(SchemaBuilder<T> parent, Expression<Func<T, TProp>> propExpr)
        {
            Parent = parent;
            PropExpr = propExpr;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : class, IScalarFieldResolver<TProp>
        {
            return Parent.GraphType<T>(
                b => b.LooseScalarField(PropExpr)
                    .ResolvesVia<TResolver>()
            );
        }
    }
}