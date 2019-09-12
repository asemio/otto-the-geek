using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek.Core
{
    public interface IListQueryFieldResolver<TElem>
    {
        Task<IEnumerable<TElem>> Resolve();
    }
}