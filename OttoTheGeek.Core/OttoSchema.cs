using GraphQL.Types;

namespace OttoTheGeek.Core
{
    public sealed class OttoSchema
    {
        public OttoSchema(IObjectGraphType queryType)
        {
            QueryType = queryType;
        }
        public IObjectGraphType QueryType { get; }
    }
}