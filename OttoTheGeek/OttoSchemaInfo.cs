using System.Collections.Generic;
using GraphQL.Types;

namespace OttoTheGeek
{
    public sealed class OttoSchemaInfo
    {
        public OttoSchemaInfo(
            IObjectGraphType queryType,
            IObjectGraphType mutationType,
            IObjectGraphType subscriptionType,
            IEnumerable<IGraphType> otherTypes)
        {
            QueryType = queryType;
            MutationType = mutationType;
            SubscriptionType = subscriptionType;
            OtherTypes = otherTypes;
        }

        public IObjectGraphType QueryType { get; }
        public IObjectGraphType MutationType { get; }
        public IObjectGraphType SubscriptionType { get; }
        public IEnumerable<IGraphType> OtherTypes { get; }
    }
}
