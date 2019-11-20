using System.Threading.Tasks;

namespace OttoTheGeek.Sample
{
    public sealed class ChildResolver : IScalarFieldResolver<Child>
    {
        private readonly ChildRepository _repo;

        public ChildResolver(ChildRepository repo)
        {
            _repo = repo;
        }

        public Task<Child> Resolve()
        {
            return Task.FromResult(_repo.GetChild());
        }
    }

    // this is an example to demonstrate a resolver leveraging other types that are activated
    // via dependency injection
    public sealed class ChildRepository
    {
        public Child GetChild() => new Child();
    }
}
