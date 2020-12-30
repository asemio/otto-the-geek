using System;
using System.Linq;
using GraphQL.Types;

namespace OttoTheGeek
{
    public class ModelSchema<TModel> : Schema
    {
        public ModelSchema(OttoSchemaInfo schema, IServiceProvider provider)
            : base(provider)
        {
            if(schema.QueryType?.Fields?.Any() == true)
            {
                Query = schema.QueryType;
            }
            if(schema.MutationType?.Fields?.Any() == true)
            {
                Mutation = schema.MutationType;
            }
            if(schema.SubscriptionType?.Fields?.Any() == true)
            {
                Subscription = schema.SubscriptionType;
            }

            RegisterTypes(schema.OtherTypes.ToArray());
        }
    }
}