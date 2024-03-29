using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

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
            where TResolver : class, ILooseScalarFieldResolver<TProp>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ScalarResolverConfiguration<TResolver, TProp>());
        }

        public LooseScalarFieldWithArgsBuilder<TModel, TProp, TArgs> WithArgs<TArgs>() where TArgs : class
        {
            return new LooseScalarFieldWithArgsBuilder<TModel, TProp, TArgs>(_parentBuilder, _prop);
        }
        public GraphTypeBuilder<TModel> Preloaded()
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new PreloadedScalarResolverConfiguration<TModel, TProp>());
        }
    }

    public sealed class LooseScalarFieldWithArgsBuilder<TModel, TProp, TArgs>
        where TModel : class where TArgs : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal LooseScalarFieldWithArgsBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, ILooseScalarFieldWithArgsResolver<TProp, TArgs>
        {
            return _parentBuilder.WithResolverConfiguration(_prop, new ScalarWithArgsResolverConfiguration<TResolver, TProp, TArgs>())
                .WithTypeConfig(x => x.ConfigureField(_prop, cfg => cfg with { ArgumentsType = typeof(TArgs) }));
        }
    }
}
