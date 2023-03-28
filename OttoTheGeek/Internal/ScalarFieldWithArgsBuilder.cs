using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.Internal
{
    public sealed class ScalarFieldWithArgsBuilder<TModel, TProp, TArgs>
        where TModel : class
        where TArgs : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal ScalarFieldWithArgsBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IScalarFieldWithArgsResolver<TModel, TProp, TArgs>
        {
            return _parentBuilder.WithResolverConfiguration(_prop,
                    new ScalarContextWithArgsResolverConfiguration<TResolver, TModel, TProp, TArgs>())
                ;
        }
    }
}
