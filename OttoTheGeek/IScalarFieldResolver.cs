using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface IScalarFieldResolver<TContext, TField>
    {
        object GetKey(TContext context);
        Task<Dictionary<object, TField>> GetData(IEnumerable<object> keys);
    }

    [Obsolete("This interface will be removed in version 1.0; use ILooseScalarFieldResolver<TProp>")]
    public interface IScalarFieldResolver<TProp> : ILooseScalarFieldResolver<TProp> {}
}