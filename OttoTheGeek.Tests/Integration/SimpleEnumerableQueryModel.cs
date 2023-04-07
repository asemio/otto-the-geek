using System.Collections.Generic;

namespace OttoTheGeek.Tests.Integration
{
    public class SimpleEnumerableQueryModel<T>
    {
        public IEnumerable<T> Children { get; set; }
    }
}