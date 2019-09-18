using System.Collections.Generic;

namespace OttoTheGeek.Connections
{
    public class Connection<TElem>
    {
        public int TotalCount { get; set; }

        public IEnumerable<TElem> Records { get; set; }
    }
}