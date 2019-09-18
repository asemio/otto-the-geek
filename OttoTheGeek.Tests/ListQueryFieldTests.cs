using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class ListQueryFieldTests
    {
        public sealed class Query
        {
            public IEnumerable<ChildObject> Children { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.ListQueryField(x => x.Children)
                    .ResolvesVia<ChildrenResolver>();
            }
        }

        public sealed class ChildrenResolver : IListQueryFieldResolver<ChildObject>
        {
            public static IEnumerable<ChildObject> Data => new[] {
                new ChildObject {
                    Value1 = "one",
                    Value2 = "uno",
                    Value3 = 1
                },
                new ChildObject {
                    Value1 = "two",
                    Value2 = "dos",
                    Value3 = 2
                }
            };
            public Task<IEnumerable<ChildObject>> Resolve()
            {
                return Task.FromResult(Data);
            }
        }

        public sealed class ChildObject
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public int Value3 { get; set; }
        }

        [Fact]
        public void BuildsSchemaType()
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
                            kind
                            ofType {
                                name
                                kind
                            }
                        }
                    }
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Object,
                Name = "Query",
                Fields = new [] {
                    new ObjectField
                    {
                        Name = "children",
                        Type = ObjectType.ListOf(
                            new ObjectType {
                                Name = "ChildObject",
                                Kind = ObjectKinds.Object
                            }
                        )
                    }
                }
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        [Fact]
        public void ReturnsObjectValues()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    value1
                    value2
                    value3
                }
            }");

            var result = rawResult["children"].ToObject<ChildObject[]>();


            result.Should().BeEquivalentTo(ChildrenResolver.Data);
        }
    }
}