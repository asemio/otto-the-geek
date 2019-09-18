using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IListQueryFieldResolver<TElem>
    {
        Task<IEnumerable<TElem>> Resolve();
    }
}