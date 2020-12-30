using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class ScalarListQueryFieldTests
    {
        public sealed class Query
        {
            public IEnumerable<string> Things { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Query>(
                    b => b.LooseListField(x => x.Things)
                        .ResolvesVia<Resolver>()
                        );
            }
        }

        public sealed class Resolver : ILooseListFieldResolver<string>
        {
            public static IEnumerable<string> Data => new[] {
                "one",
                "two",
                "three"
            };
            public Task<IEnumerable<string>> Resolve()
            {
                return Task.FromResult(Data);
            }
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
                                ofType {
                                    name
                                    kind
                                }
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
                        Name = "things",
                        Type = ObjectType.ListOf(
                            ObjectType.NonNullableOf(ObjectType.String)
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
                things
            }");

            var result = rawResult["things"].ToObject<string[]>();


            result.Should().BeEquivalentTo(Resolver.Data);
        }
    }
}