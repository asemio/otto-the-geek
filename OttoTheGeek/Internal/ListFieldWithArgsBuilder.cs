using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.Internal
{
    public sealed class ListFieldWithArgsBuilder<TModel, TElem, TArgs>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;
        private readonly ScalarTypeMap _scalarTypeMap;

        internal ListFieldWithArgsBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop, ScalarTypeMap scalarTypeMap)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
            _scalarTypeMap = scalarTypeMap;
        }
        
        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldWithArgsResolver<TModel, TElem, TArgs>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ListContextWithArgsResolverConfiguration<TResolver, TModel, TElem, TArgs>());
        }
    }
}