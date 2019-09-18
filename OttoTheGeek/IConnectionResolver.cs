using System.Threading.Tasks;
using OttoTheGeek.Connections;

namespace OttoTheGeek
{
    public interface IConnectionResolver<TElem>
    {
        Task<Connection<TElem>> Resolve(PagingArgs args);
    }
}