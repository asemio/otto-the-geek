using System.Linq;
using GraphQL.Types;

namespace OttoTheGeek
{
    public sealed class ModelSchema<TQuery> : Schema
    {
        public ModelSchema(OttoSchema schema)
        {
            Query = schema.QueryType;
            RegisterTypes(schema.OtherTypes.ToArray());
        }
    }
}