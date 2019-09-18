using System.Collections.Generic;

namespace OttoTheGeek
{
    public class ObjectType
    {
        public static readonly ObjectType String = new ObjectType { Name = "String", Kind = ObjectKinds.Scalar };
        public static readonly ObjectType Int = new ObjectType { Name = "Int", Kind = ObjectKinds.Scalar };

        public static ObjectType NonNullableOf(ObjectType innerType)
        {
            return new ObjectType {
                Kind = ObjectKinds.NonNull,
                OfType = innerType
            };
        }

        public static ObjectType ListOf(ObjectType innerType)
        {
            return new ObjectType {
                Kind = ObjectKinds.List,
                OfType = innerType
            };
        }

        public string Name { get; set; }
        public string Kind { get; set; }

        public ObjectType OfType { get; set; }

        public IEnumerable<ObjectField> Fields { get; set; }

    }

    public static class ObjectKinds
    {
        public const string Scalar = "SCALAR";
        public const string Object = "OBJECT";
        public const string NonNull = "NON_NULL";
        public const string List = "LIST";
    }
}