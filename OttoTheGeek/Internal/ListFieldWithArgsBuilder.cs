using System.Reflection;
using OttoTheGeek.Internal.ResolverConfiguration;

namespace OttoTheGeek.Internal
{
    public sealed class ListFieldWithArgsBuilder<TModel, TElem, TArgs>
        where TModel : class
    {
        private readonly GraphTypeBuilder<TModel> _parentBuilder;
        private readonly PropertyInfo _prop;

        internal ListFieldWithArgsBuilder(GraphTypeBuilder<TModel> parentBuilder, PropertyInfo prop)
        {
            _parentBuilder = parentBuilder;
            _prop = prop;
        }

        public GraphTypeBuilder<TModel> ResolvesVia<TResolver>()
            where TResolver : class, IListFieldWithArgsResolver<TModel, TElem, TArgs>
        {
            return _parentBuilder
                .WithResolverConfiguration(_prop, new ListContextWithArgsResolverConfiguration<TResolver, TModel, TElem, TArgs>())
                .WithTypeConfig(cfg => cfg.ConfigureField(_prop, fld => fld with { ArgumentsType = typeof(TArgs) }));
        }
    }
}
