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
        }

        public sealed class Args
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
        }

        public sealed class Resolver : IScalarFieldWithArgsResolver<Child, Args>
        {
            public async Task<Child> Resolve(Args args)
            {
                await Task.CompletedTask;
                return new Child {
                    AnInt = args.AnInt,
                    ANullableInt = args.ANullableInt
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
                            }
                        }
                    },
                },
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        [Fact]
        public void DeserializesArgs()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                child(anInt: 7) {
                    anInt
                }
            }");

            var result = rawResult["child"].ToObject<Child>();

            result.Should().BeEquivalentTo(new Child {
                AnInt = 7
            });
        }
    }
}