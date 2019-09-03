using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Core.Tests
{
    public sealed class ScalarQueryFieldTests
    {
        public sealed class Query
        {
            public string StringVal { get; set; }
            public int IntVal { get; set; }
            public long LongVal { get; set; }
            public long? NullableLongVal { get; set; }
        }

        [Fact]
        public void RoundTripsFields()
        {
            var query = new Query {
                StringVal = "hello world",
                IntVal = 22,
                LongVal = (long)0x700000000,
                NullableLongVal = 7
            };
            var model = new OttoModel<Query>();
            var server = model.CreateServer(query);

            var result = server.Execute<Query>(@"query {
                stringVal
                intVal
                longVal
                nullableLongVal
            }");

            result.Should().BeEquivalentTo(query);
        }

        [Fact]
        public void GeneratesSchema()
        {
            var query = new Query {
                StringVal = "hello world",
                IntVal = 7,
            };
            var model = new OttoModel<Query>();
            var server = model.CreateServer(query);

            var result = server.Execute<JObject>(@"query {
                __type(name:""Query"") {
                    name
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

            var expected = new ObjectType
            {
                Name = "Query",
                Fields = new[] {
                    new ObjectField {
                        Name = "stringVal",
                        Type = ObjectType.NonNullableOf(ObjectType.String)
                    },
                    new ObjectField {
                        Name = "intVal",
                        Type = ObjectType.NonNullableOf(ObjectType.Int)
                    },
                    new ObjectField {
                        Name = "longVal",
                        Type = ObjectType.NonNullableOf(ObjectType.Int)
                    },
                    new ObjectField {
                        Name = "nullableLongVal",
                        Type = ObjectType.Int
                    },
                }
            };

            var actual = result["__type"].ToObject<ObjectType>();

            foreach(var field in expected.Fields)
            {
                actual.Fields.Should().ContainEquivalentOf(field);
            }

            actual.Fields.Should().BeEquivalentTo(expected.Fields);
        }
    }

}
