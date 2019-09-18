using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class ListQueryFieldBuilder<T, TElem>
        where TElem : class
    {
        private readonly GraphTypeBuilder<TElem> _builder;
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;

        internal ListQueryFieldBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo, GraphTypeBuilder<TElem> builder)
        {
            _builder = builder;
            _parent = parent;
            _propertyInfo = propertyInfo;
        }
        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IListQueryFieldResolver<TElem>
        {
            return _parent.WithGraphTypeBuilder(
                _builder.WithListQueryFieldResolver<TResolver>()
                );
        }
    }
}