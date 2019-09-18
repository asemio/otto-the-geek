using System.Collections.Generic;
using System.Reflection;

namespace OttoTheGeek
{
    public class QueryFieldBuilder<T, TProp>
        where TProp : class
    {
        private readonly SchemaBuilder<T> _parent;
        private readonly PropertyInfo _propertyInfo;
        private readonly GraphTypeBuilder<TProp> _builder;

        internal QueryFieldBuilder(SchemaBuilder<T> parent, PropertyInfo propertyInfo, GraphTypeBuilder<TProp> builder)
        {
            _parent = parent;
            _propertyInfo = propertyInfo;
            _builder = builder;
        }

        public SchemaBuilder<T> ResolvesVia<TResolver>()
            where TResolver : IQueryFieldResolver<TProp>
        {
            return _parent.WithGraphTypeBuilder(
                _builder.WithScalarQueryFieldResolver<TResolver>()
                );
        }
    }
}