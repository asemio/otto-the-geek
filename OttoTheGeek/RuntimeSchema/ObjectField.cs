using System.Collections.Generic;

namespace OttoTheGeek.RuntimeSchema
{
    public sealed class ObjectField
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ObjectType Type { get; set; }

        public IEnumerable<FieldArgument> Args { get; set; }
    }
}