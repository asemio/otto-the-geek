using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.Connections;
using OttoTheGeek.RuntimeSchema;

namespace OttoTheGeek.Tests
{
    public sealed class CustomNamedConnectionTests
    {
        public const string ConnectionTypeName = "CustomChildConnection";
        public sealed class Query
        {
            public IEnumerable<ChildObject> Children { get; set; }
        }

        public class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.ConnectionField(x => x.Children)
                    .ResolvesVia<ChildrenResolver>()
                    .GraphType<Connection<ChildObject>>(x => x.Named(ConnectionTypeName));

            }
        }

        public sealed class ChildrenResolver : IConnectionResolver<ChildObject>
        {
            public Task<Connection<ChildObject>> Resolve(PagingArgs<ChildObject> args)
            {
                throw new System.NotImplementedException();
            }
        }

        public sealed class ChildObject
        {
            public string Value1 { get; set; }
        }

        public void HasCustomName()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                __type(name:""Query"") {
                    name
                    kind
                    fields {
                        name
                        type {
                            name
                        }
                    }
                }
            }");

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Fields
                .Single()
                .Type
                .Name
                .Should()
                .Be(ConnectionTypeName);
        }

    }
}