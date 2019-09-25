using System.Collections.Generic;
using GraphQL.Types;

namespace OttoTheGeek
{
    public sealed class OttoSchema
    {
        public OttoSchema(IObjectGraphType queryType, IEnumerable<IGraphType> otherTypes)
        {
            QueryType = queryType;
            OtherTypes = otherTypes;
        }
        public IObjectGraphType QueryType { get; }
        public IEnumerable<IGraphType> OtherTypes { get; }
    }
}