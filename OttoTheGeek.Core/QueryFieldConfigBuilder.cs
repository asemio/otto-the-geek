using System.Reflection;

namespace OttoTheGeek.Core
{
    public sealed class QueryFieldConfigBuilder<T, TProp>
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;

        public QueryFieldConfigBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo)
        {
            _parent = parent;
            _propertyInfo = propertyInfo;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IQueryFieldResolver<TProp>
        {
            return _parent.WithQueryFieldResolver(typeof(IQueryFieldResolver<TProp>), typeof(TResolver));
        }
    }
}