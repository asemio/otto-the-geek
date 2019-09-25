using System;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public sealed class TimeSpanGraphType : StringGraphType
    {
        public TimeSpanGraphType()
        {
            Name = "TimeSpan";
            Description = "A TimeSpan value serialized as an ISO 8601 duration.";
        }

        public override object Serialize(object value)
        {
            var ts = (TimeSpan)value;

            return System.Xml.XmlConvert.ToString(ts);
        }
    }
}