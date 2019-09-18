using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IQueryFieldResolver<TProp>
    {
        Task<TProp> Resolve();
    }
}