using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public sealed class OttoEnumGraphType<TEnum> : EnumerationGraphType
    {
        public OttoEnumGraphType()
        {
            Name = typeof(TEnum).Name;
            foreach(var val in typeof(TEnum).GetEnumValues())
            {
                AddValue(typeof(TEnum).GetEnumName(val), null, val);
            }
        }
    }
}