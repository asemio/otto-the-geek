using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

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
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedListResolverConfiguration<TModel, TElem>());
        }

        public ListFieldWithArgsBuilder<TModel, TElem, TArgs> WithArgs<TArgs>()
        {
            return new ListFieldWithArgsBuilder<TModel, TElem, TArgs>(_parentBuilder, _prop);
        }
    }
}
