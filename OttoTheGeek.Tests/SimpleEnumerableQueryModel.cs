using System.Collections.Generic;

namespace OttoTheGeek.Tests
{
    public class SimpleEnumerableQueryModel<T>
    {
        public IEnumerable<T> Children { get; set; }
    }
}