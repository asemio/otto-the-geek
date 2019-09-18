using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IScalarFieldResolver<TContext, TField>
    {
        object GetKey(TContext context);
        Task<Dictionary<object, TField>> GetData(IEnumerable<object> keys);
    }
}