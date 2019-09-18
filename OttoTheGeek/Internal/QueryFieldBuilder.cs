using System.Reflection;

namespace OttoTheGeek.Internal
{
    public class QueryFieldBuilder<T, TProp>
        where TProp : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;

        internal QueryFieldBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo)
        {
            _parent = parent;
            _propertyInfo = propertyInfo;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IQueryFieldResolver<TProp>
        {
            return _parent.GraphType<TProp>(
                b => b.WithScalarQueryFieldResolver<TResolver>()
                );
        }
    }
}