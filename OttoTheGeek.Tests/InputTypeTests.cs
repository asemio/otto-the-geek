using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class InputTypeTests
    {
        public sealed class Query
        {
            public Child Child { get; set; }
        }

        public enum Texture
        {
            Crunchy,
            Smooth,
            Chunky,
            Grainy
        }

        public sealed class Child
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
            public IEnumerable<int> ListOfInts { get; set; }
        }

        public sealed class Args
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
            public IEnumerable<int> ListOfInts { get; set; }
        }

        public sealed class Resolver : IScalarFieldWithArgsResolver<Child, Args>
        {
            public async Task<Child> Resolve(Args args)
            {
                await Task.CompletedTask;
                return new Child {
                    AnInt = args.AnInt,
                    ANullableInt = args.ANullableInt,
                    Texture = args.Texture,
                    ListOfInts = args.ListOfInts
                };
            }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.QueryField(x => x.Child)
                    .WithArgs<Args>()
                    .ResolvesVia<Resolver>()
                    .GraphType<Args>(b => b.IgnoreProperty(p => p.IgnoredString))
                    .GraphType<Child>(b => b.ListField(x => x.ListOfInts).Preloaded())
                    ;
            }
        }

        [Fact]
        public void ConfiguresSchema()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                __type(name:""Query"") {
                    name
                    kind
                    fields {
                        name
                        args {
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
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Object,
                Name = "Query",
                Fields = new [] {
                    new ObjectField
                    {
                        Name = "child",
                        Args = new [] {
                            new FieldArgument
                            {
                                Name = nameof(Args.AnInt).ToCamelCase(),
                                Type = ObjectType.NonNullableOf(ObjectType.Int)
                            },
                            new FieldArgument
                            {
                                Name = nameof(Args.ANullableInt).ToCamelCase(),
                                Type = ObjectType.Int
                            },
                            new FieldArgument
                            {
                                Name = nameof(Args.Texture).ToCamelCase(),
                                Type = new ObjectType {
                                    Name = "Texture",
                                    Kind = ObjectKinds.Enum
                                }
                            },
                            new FieldArgument
                            {
                                Name = nameof(Args.ListOfInts).ToCamelCase(),
                                Type = ObjectType.ListOf(
                                            ObjectType.NonNullableOf(
                                                ObjectType.Int))
                            },
                        }
                    },
                },
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        [Fact]
        public void ConfiguresEnumValues()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                __type(name:""Texture"") {
                    name
                    kind
                    enumValues {
                        name
                    }
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Enum,
                Name = "Texture",
                EnumValues = new [] {
                    new EnumValue { Name = "Crunchy" },
                    new EnumValue { Name = "Smooth" },
                    new EnumValue { Name = "Chunky" },
                    new EnumValue { Name = "Grainy" }
                }
            };

            var fieldType = rawResult["__type"].ToObject<ObjectType>();

            fieldType.Should().BeEquivalentTo(expectedType);
        }

        public static IEnumerable<object[]> DeserializesArgsData()
        {
            yield return new object[] { new Args { AnInt = 4, ANullableInt = null } };

            yield return new object[] { new Args { AnInt = 7, ANullableInt = 20, ListOfInts = new[] { 4, 5, 6, 0 } } };

            yield return new object[] { new Args { AnInt = 4, Texture = Texture.Chunky } };
        }

        [Theory]
        [MemberData(nameof(DeserializesArgsData))]
        public void DeserializesArgs(Args args)
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"
            query ($anInt: Int!, $aNullableInt: Int, $texture: Texture, $listOfInts: [Int!]) {
                child(anInt: $anInt, aNullableInt: $aNullableInt, texture: $texture, listOfInts: $listOfInts) {
                    anInt
                    aNullableInt
                    texture
                    listOfInts
                }
            }", new {
                anInt = args.AnInt,
                aNullableInt = args.ANullableInt,
                texture = args.Texture?.ToString(),
                listOfInts = args.ListOfInts
            });

            var result = rawResult["child"].ToObject<Child>();

            result.Should().BeEquivalentTo(args);
        }
    }
}