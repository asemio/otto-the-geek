using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface ILooseListFieldWithArgsResolver<TElem, TArgs>
    {
        Task<IEnumerable<TElem>> Resolve(TArgs args);
    }
}