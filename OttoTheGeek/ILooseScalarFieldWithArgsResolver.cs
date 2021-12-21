using System.Threading.Tasks;

namespace OttoTheGeek
{
    public interface ILooseScalarFieldWithArgsResolver<TProp, TArgs>
    {
        public Task<TProp> Resolve(TArgs args);
    }
}
