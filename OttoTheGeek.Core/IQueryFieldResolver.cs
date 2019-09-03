using System.Threading.Tasks;

namespace OttoTheGeek.Core
{
    public interface IQueryFieldResolver<TProp>
    {
        Task<TProp> Resolve();
    }
}