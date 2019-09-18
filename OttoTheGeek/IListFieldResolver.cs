using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IListFieldResolver<TContext, TField>
    {
        object GetKey(TContext context);
        Task<ILookup<object, TField>> GetData(IEnumerable<object> keys);
    }
}