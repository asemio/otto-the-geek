using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class ListFieldBuilder<TModel, TElem>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal ListFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldResolver<TModel, TElem>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ListContextResolverConfiguration<TResolver, TModel, TElem>());
        }

        public GraphTypeBuilder<TModel> Preloaded()
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedListResolverConfiguration<TElem>());
        }
    }

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
            where TResolver : class, IListFieldResolver<TElem>
        {
            var prop = _propExpr.PropertyInfoForSimpleGet();
            return _parentBuilder.WithResolverConfiguration(prop, new ListResolverConfiguration<TResolver, TElem>());
        }
    }
}