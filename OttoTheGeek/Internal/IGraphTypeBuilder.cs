using OttoTheGeek.TypeModel;

namespace OttoTheGeek.Internal
{
    public interface IGraphTypeBuilder
    {
        // indicates whether the type represented by this builder needs to be
        // registered in the schema via RegisterType()

        OttoTypeConfig TypeConfig { get; }
    }
}
