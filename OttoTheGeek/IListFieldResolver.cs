using System;
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

    [Obsolete("This interface will be removed in version 2.0; use ILooseListFieldResolver<TElem>")]
    public interface IListFieldResolver<TElem> : ILooseListFieldResolver<TElem> {}
}
