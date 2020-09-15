using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OttoTheGeek.Internal
{
    public sealed class LooseListFieldBuilder<TModel, TElem>
        where TModel : class
    {
        private readonly ScalarTypeMap _scalarTypeMap;
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly Expression<Func<TModel, IEnumerable<TElem>>> _propExpr;

        internal LooseListFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, Expression<Func<TModel, IEnumerable<TElem>>> propExpr, ScalarTypeMap scalarTypeMap)
        {
            _scalarTypeMap = scalarTypeMap;
            _parentBuilder = parentBuilder;
            _propExpr = propExpr;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldResolver<TElem>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new ListResolverConfiguration<TResolver, TElem>());
        }

        public LooseListFieldWithArgsBuilder<TModel, TElem, TArgs> WithArgs<TArgs>()
        {
            return new LooseListFieldWithArgsBuilder<TModel, TElem, TArgs>(_parentBuilder, _propExpr);
        }

        public GraphTypeBuilder<TModel> Preloaded()
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new PreloadedListResolverConfiguration<TElem>(_scalarTypeMap));
        }
    }

    public sealed class LooseListFieldWithArgsBuilder<TModel, TElem, TArgs>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly Expression<Func<TModel, IEnumerable<TElem>>> _propExpr;

        internal LooseListFieldWithArgsBuilder(GraphTypeBuilder<TModel> parentBuilder, Expression<Func<TModel, IEnumerable<TElem>>> propExpr)
        {
            _parentBuilder = parentBuilder;
            _propExpr = propExpr;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldWithArgsResolver<TElem, TArgs>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new ListWithArgsResolverConfiguration<TResolver, TElem, TArgs>());
        }
    }
}