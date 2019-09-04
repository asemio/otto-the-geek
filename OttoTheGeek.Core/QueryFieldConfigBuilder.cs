using System.Reflection;

namespace OttoTheGeek.Core
{
    public sealed class QueryFieldConfigBuilder<T, TProp>
        where TProp : class
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
            return _parent.WithGraphTypeBuilder(
                new GraphTypeBuilder<TProp>().WithScalarQueryFieldResolver<TResolver>()
                );
        }
    }
}