using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IListFieldWithArgsResolver<TElem, TArgs>
    {
        Task<IEnumerable<TElem>> Resolve(TArgs args);
    }
}