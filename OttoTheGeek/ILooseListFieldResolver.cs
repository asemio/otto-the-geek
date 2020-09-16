using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface ILooseListFieldResolver<TElem>
    {
        Task<IEnumerable<TElem>> Resolve();
    }
}