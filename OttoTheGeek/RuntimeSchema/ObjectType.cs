using System.Collections.Generic;

namespace OttoTheGeek.RuntimeSchema
{
    public sealed class ObjectType
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

        public static ObjectType ConnectionOf(ObjectType innerType)
        {
            return new ObjectType {
                Kind = ObjectKinds.Object,
                Name = $"{innerType.Name}Connection",
                Fields = new[] {
                    new ObjectField {
                        Name = "totalCount",
                        Type = NonNullableOf(ObjectType.Int),
                    },
                    new ObjectField {
                        Name = "records",
                        Type = ObjectType.ListOf(innerType)
                    }
                }
            };
        }

        public string Name { get; set; }
        public string Kind { get; set; }

        public ObjectType OfType { get; set; }

        public IEnumerable<ObjectField> Fields { get; set; }

        public IEnumerable<EnumValue> EnumValues { get; set; }
    }

    public sealed class EnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }

    }
}