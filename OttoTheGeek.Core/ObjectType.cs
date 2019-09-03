using System.Collections.Generic;

namespace OttoTheGeek.Core
{
    public class ObjectType
    {
        public static readonly ObjectType String = new ObjectType { Name = "String", Kind = "SCALAR" };
        public static readonly ObjectType Int = new ObjectType { Name = "Int", Kind = "SCALAR" };

        public static ObjectType NonNullableOf(ObjectType innerType)
        {
            return new ObjectType {
                Kind = "NON_NULL",
                OfType = innerType
            };
        }

        public string Name { get; set; }
        public string Kind { get; set; }

        public ObjectType OfType { get; set; }

        public IEnumerable<ObjectField> Fields { get; set; }

    }
}