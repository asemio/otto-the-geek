using System.Reflection;

namespace OttoTheGeek
{
    public sealed class ScalarFieldBuilder<TModel, TProp>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal ScalarFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : IScalarFieldResolver<TModel, TProp>
        {
            return _parentBuilder.WithScalarFieldResolver<TProp, TResolver>(_prop);
        }
    }
}