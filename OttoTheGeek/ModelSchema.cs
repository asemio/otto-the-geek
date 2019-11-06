using System.Linq;
using GraphQL.Types;

namespace OttoTheGeek
{
    public sealed class ModelSchema : Schema
    {
        public ModelSchema(OttoSchemaInfo schema)
        {
            Query = schema.QueryType;
            Mutation = schema.MutationType;
            Subscription = schema.SubscriptionType;
            RegisterTypes(schema.OtherTypes.ToArray());
        }
    }
}