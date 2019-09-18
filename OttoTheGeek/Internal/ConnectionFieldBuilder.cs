using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class ConnectionFieldBuilder<T, TElem>
        where TElem : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;

        internal ConnectionFieldBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo)
        {
            _parent = parent;
            _propertyInfo = propertyInfo;
        }
        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IConnectionResolver<TElem>
        {
            return _parent.GraphType<TElem>(
                b => b.WithConnectionResolver<TResolver>()
                ).ConnectionProperty(_propertyInfo);
        }
    }
}