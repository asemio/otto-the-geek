using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IScalarFieldWithArgsResolver<TProp, TArgs>
    {
        Task<TProp> Resolve(TArgs args);
    }
}