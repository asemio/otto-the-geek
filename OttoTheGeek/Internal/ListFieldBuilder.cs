using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

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
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedListResolverConfiguration<TModel, TElem>(_scalarTypeMap));
        }

        public ListFieldWithArgsBuilder<TModel, TElem, TArgs> WithArgs<TArgs>()
        {
            return new ListFieldWithArgsBuilder<TModel, TElem, TArgs>(_parentBuilder, _prop, _scalarTypeMap);
        }
    }
}
