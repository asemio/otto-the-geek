using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.Internal
{
    public sealed class LooseListFieldBuilder<TModel, TElem>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly Expression<Func<TModel, IEnumerable<TElem>>> _propExpr;

        internal LooseListFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, Expression<Func<TModel, IEnumerable<TElem>>> propExpr)
        {
            _parentBuilder = parentBuilder;
            _propExpr = propExpr;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, ILooseListFieldResolver<TElem>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new LooseListResolverConfiguration<TResolver, TElem>());
        }

        public LooseListFieldWithArgsBuilder<TModel, TElem, TArgs> WithArgs<TArgs>()
        {
            return new LooseListFieldWithArgsBuilder<TModel, TElem, TArgs>(_parentBuilder, _propExpr);
        }

        public GraphTypeBuilder<TModel> Preloaded()
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new PreloadedListResolverConfiguration<TModel, TElem>());
        }
    }

    public sealed class LooseListFieldWithArgsBuilder<TModel, TElem, TArgs>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly Expression<Func<TModel, IEnumerable<TElem>>> _propExpr;

        internal LooseListFieldWithArgsBuilder(
            GraphTypeBuilder<TModel> parentBuilder,
            Expression<Func<TModel, IEnumerable<TElem>>> propExpr
            )
        {
            _parentBuilder = parentBuilder;
            _propExpr = propExpr;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, ILooseListFieldWithArgsResolver<TElem, TArgs>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new LooseListWithArgsResolverConfiguration<TResolver, TElem, TArgs>())
                .WithTypeConfig(cfg => cfg.ConfigureField(prop, fld => fld with { ArgumentsType = typeof(TArgs) }));
        }
    }
}
