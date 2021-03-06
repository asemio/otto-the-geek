using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public abstract class FieldWithArgsResolverConfiguration<TArgs> : FieldResolverConfiguration
    {
        protected override QueryArguments GetQueryArguments(GraphTypeCache cache, IServiceCollection services)
        {
            return cache.GetOrCreateArguments<TArgs>(services);
        }
    }
}