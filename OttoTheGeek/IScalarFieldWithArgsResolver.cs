using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IScalarFieldWithArgsResolver<TContext, TProp, TArgs>
    {
        object GetKey(TContext context);
        Task<IDictionary<object, TProp>> GetData(IEnumerable<object> keys, TArgs args);
    }

    [Obsolete("This interface will be removed in version 2.0; use ILooseListFieldResolver<TElem, TArgs>")]
    public interface IScalarFieldWithArgsResolver<TProp, TArgs> : ILooseScalarFieldWithArgsResolver<TProp, TArgs>
    {
    }
}
