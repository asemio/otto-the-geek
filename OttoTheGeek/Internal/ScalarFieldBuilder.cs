using System.Reflection;
using GraphQL.Types;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.Internal
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
            where TResolver : class, IScalarFieldResolver<TModel, TProp>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ScalarContextResolverConfiguration<TResolver, TModel, TProp>());
        }

        public GraphTypeBuilder<TModel> Preloaded()
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedScalarResolverConfiguration<TModel, TProp>());
        }

        public GraphTypeBuilder<TModel> AsGraphType<TGraphType>()
            where TGraphType : IGraphType
        {
            return _parentBuilder.WithGraphTypeOverride(_prop, typeof(TGraphType));
        }
    }
}
