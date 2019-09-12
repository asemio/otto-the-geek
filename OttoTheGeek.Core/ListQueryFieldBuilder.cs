using System.Reflection;

namespace OttoTheGeek.Core
{
    public sealed class ListQueryFieldBuilder<T, TElem>
        where TElem : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;

        internal ListQueryFieldBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo)
        {
            _parent = parent;
            _propertyInfo = propertyInfo;
        }
        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IListQueryFieldResolver<TElem>
        {
            return _parent.WithGraphTypeBuilder(
                new GraphTypeBuilder<TElem>().WithListQueryFieldResolver<TResolver>()
                );
        }
    }
}