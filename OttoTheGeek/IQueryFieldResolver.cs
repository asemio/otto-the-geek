using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IScalarFieldResolver<TProp>
    {
        Task<TProp> Resolve();
    }
}