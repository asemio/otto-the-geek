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
        private readonly ScalarTypeMap _scalarTypeMap;

        internal ListFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop, ScalarTypeMap scalarTypeMap)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
            _scalarTypeMap = scalarTypeMap;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldResolver<TModel, TElem>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ListContextResolverConfiguration<TResolver, TModel, TElem>());
        }

        public GraphTypeBuilder<TModel> Preloaded()
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedListResolverConfiguration<TElem>(_scalarTypeMap));
        }
    }

}