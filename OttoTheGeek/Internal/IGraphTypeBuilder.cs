using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public interface IGraphTypeBuilder
    {
        // indicates whether the type represented by this builder needs to be
        // registered in the schema via RegisterType()
        bool NeedsRegistration { get; }
        string GraphTypeName { get; }
        IComplexGraphType BuildGraphType(GraphTypeCache cache, IServiceCollection services);
    }
}
