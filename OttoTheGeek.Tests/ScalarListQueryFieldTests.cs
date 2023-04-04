using System.Collections.Generic;
using System.Linq;
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
            public IEnumerable<string> ThingsWithArgs { get; set; }
            public IEnumerable<int> NumsWithArgs { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Query>(
                    b => b
                        .LooseListField(x => x.Things)
                            .ResolvesVia<ThingsResolver>()
                        .LooseListField(x => x.ThingsWithArgs)
                            .WithArgs<Args>()
                            .ResolvesVia<ThingsWithArgsResolver>()
                        .LooseListField(x => x.NumsWithArgs)
                            .WithArgs<Args>()
                            .ResolvesVia<NumsWithArgsResolver>()
                        );
            }
        }

        public sealed class ThingsResolver : ILooseListFieldResolver<string>
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
        
        public class Args
        {
            public string Arg { get; set; }
        }
        public sealed class ThingsWithArgsResolver : ILooseListFieldWithArgsResolver<string, Args>
        {
            public static IEnumerable<string> Data => new[] {
                "one",
                "two",
                "three"
            };
            public Task<IEnumerable<string>> Resolve(Args args)
            {
                return Task.FromResult(Data);
            }
        }
        public sealed class NumsWithArgsResolver : ILooseListFieldWithArgsResolver<int, Args>
        {
            public Task<IEnumerable<int>> Resolve(Args args)
            {
                return Task.FromResult(Enumerable.Range(1, 5));
            }
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
                    },
                    new ObjectField
                    {
                        Name = "thingsWithArgs",
                        Type = ObjectType.ListOf(
                            ObjectType.NonNullableOf(ObjectType.String)
                        )
                    },
                    new ObjectField
                    {
                        Name = "numsWithArgs",
                        Type = ObjectType.ListOf(
                            ObjectType.NonNullableOf(ObjectType.Int)
                        )
                    },
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
                things
            }");

            var result = rawResult["things"].ToObject<string[]>();


            result.Should().BeEquivalentTo(ThingsResolver.Data);
        }
    }
}
