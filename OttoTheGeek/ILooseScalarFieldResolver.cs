using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface ILooseScalarFieldResolver<TProp>
    {
        Task<TProp> Resolve();
    }
}