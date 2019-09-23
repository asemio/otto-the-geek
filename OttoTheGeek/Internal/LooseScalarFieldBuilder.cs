using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class LooseScalarFieldBuilder<TModel, TProp>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal LooseScalarFieldBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IScalarFieldResolver<TProp>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ScalarResolverConfiguration<TResolver, TProp>());
        }
    }
}