using GraphQL.Types;

namespace OttoTheGeek.Core
{
    public class OttoModel<TQuery>
        where TQuery : class
    {
        protected virtual SchemaBuilder<TQuery> ConfigureSchema(SchemaBuilder<TQuery> builder) => builder;

        public OttoServer CreateServer(TQuery rootObject = null)
        {
            var builder = ConfigureSchema(new SchemaBuilder<TQuery>());

            var queryType = new ObjectGraphType {
                Name = "Query"
            };

            queryType.RegisterProperties(rootObject, builder);

            var schema = new Schema {
                Query = queryType
            };
            return new OttoServer(schema);
        }
    }
}