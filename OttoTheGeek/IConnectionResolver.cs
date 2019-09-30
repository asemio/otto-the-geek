using System.Threading.Tasks;
using OttoTheGeek.Connections;

namespace OttoTheGeek
{
    public interface IConnectionResolver<TElem, TArgs>
        where TArgs : PagingArgs<TElem>
    {
        Task<Connection<TElem>> Resolve(TArgs args);
    }

    public interface IConnectionResolver<TElem> : IConnectionResolver<TElem, PagingArgs<TElem>>
    {
    }
}