using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.Connections;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class CustomNamedConnectionTests
    {
        public const string ConnectionTypeName = "CustomChildConnection";

        public class Model : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                        b
                            .ConnectionField(x => x.Children)
                                .ResolvesVia<ChildrenResolver>()
                            .Named("Query")
                    )
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

        [Fact]
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