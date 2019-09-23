using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IListFieldResolver<TElem>
    {
        Task<IEnumerable<TElem>> Resolve();
    }
}