using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IListFieldWithArgsResolver<in TContext, TElem, in TArgs>
    {
        object GetKey(TContext context);
        Task<ILookup<object, TElem>> GetData(IEnumerable<object> keys, TArgs args);
    }

    [Obsolete("This interface will be removed in version 2.0; use ILooseListFieldResolver<TElem, TArgs>")]
    public interface IListFieldWithArgsResolver<TElem, TArgs> : ILooseListFieldWithArgsResolver<TElem, TArgs> {}
}
