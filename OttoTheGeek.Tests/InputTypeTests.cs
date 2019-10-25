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

        public sealed class Child
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
        }

        public enum Texture
        {
            Crunchy,
            Smooth,
            Chunky,
            Grainy
        }

        public sealed class Args
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
        }

        public sealed class Resolver : IScalarFieldWithArgsResolver<Child, Args>
        {
            public async Task<Child> Resolve(Args args)
            {
                await Task.CompletedTask;
                return new Child {
                    AnInt = args.AnInt,
                    ANullableInt = args.ANullableInt,
                    Texture = args.Texture
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
                    .GraphType<Args>(b => b.IgnoreProperty(p => p.IgnoredString));
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

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        public static IEnumerable<object[]> DeserializesArgsData()
        {
            yield return new object[] { new Args { AnInt = 4, ANullableInt = null } };

            yield return new object[] { new Args { AnInt = 7, ANullableInt = 20 } };

            yield return new object[] { new Args { AnInt = 4, Texture = Texture.Chunky } };
        }

        [Theory]
        [MemberData(nameof(DeserializesArgsData))]
        public void DeserializesArgs(Args args)
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"
            query ($anInt: Int!, $aNullableInt: Int, $texture: Texture) {
                child(anInt: $anInt, aNullableInt: $aNullableInt, texture: $texture) {
                    anInt
                    aNullableInt
                    texture
                }
            }", new {
                anInt = args.AnInt,
                aNullableInt = args.ANullableInt,
                texture = args.Texture?.ToString()
            });

            var result = rawResult["child"].ToObject<Child>();

            result.Should().BeEquivalentTo(args);
        }
    }
}