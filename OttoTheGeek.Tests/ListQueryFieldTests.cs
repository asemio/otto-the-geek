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
        public sealed class Model : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleEnumerableQueryModel<ChildObject>>(
                    b => b.Named("Query").LooseListField(x => x.Children)
                        .ResolvesVia<ChildrenResolver>()
                        );
            }
        }

        public sealed class ChildrenResolver : ILooseListFieldResolver<ChildObject>
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
        public async Task BuildsSchemaType()
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
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
        public async Task ReturnsObjectValues()
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
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
