using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests.Integration
{
    public sealed class NullabilityTests
    {
        public sealed class Query
        {
            public string Nullable => "some value";

            public IEnumerable<Child> Children { get; set; }
        }

        public sealed class Child
        {
            public int AValue => 1;
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<Query>(b =>
                        b.LooseListField(x => x.Children)
                            .WithArgs<Args>()
                            .ResolvesVia<ChildResolver>()
                    )
                    .GraphType<Args>(ConfigureArgs)
                    .GraphType<Query>(ConfigureQuery);
            }

            private GraphTypeBuilder<Args> ConfigureArgs(GraphTypeBuilder<Args> builder) =>
                builder
                    .NonNullable(x => x.OrderBy)
                    .Nullable(x => x.SearchText);
            private GraphTypeBuilder<Query> ConfigureQuery(GraphTypeBuilder<Query> builder) =>
                builder
                    .Nullable(x => x.Nullable);
        }

        public sealed class Args
        {
            public OrderValue<Child> OrderBy { get; set; }
            public string SearchText { get; set; }
        }

        public sealed class ChildResolver : ILooseListFieldWithArgsResolver<Child, Args>
        {
            public static IEnumerable<Child> Data = new[] {
                new Child(),
                new Child(),
                new Child(),
                new Child(),
                new Child(),
            };
            public Task<IEnumerable<Child>> Resolve(Args args)
            {
                throw new System.NotImplementedException();
            }
        }

        [Fact]
        public async Task InputTypes()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Query"") {
                    fields {
                        name
                        args {
                            name
                            type {
                                name
                                kind
                                ofType {
                                    name
                                }
                            }
                        }
                    }
                }
            }");

            var expectedEnumType = new ObjectType {
                Kind = ObjectKinds.Enum,
                Name = "ChildOrderBy",
            };

            var result = rawResult["__type"].ToObject<ObjectType>();

            result.Fields
                .Single(x => x.Name == "children")
                .Args
                .Select(x => x.Type)
                .Should().BeEquivalentTo(new[] {
                    expectedEnumType,
                    new ObjectType {
                        Name = "String",
                        Kind = ObjectKinds.Scalar
                    }
                });
        }

        [Fact]
        public async Task OutputTypes()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Query"") {
                    fields {
                        name
                        type {
                            name
                            kind
                        }
                    }
                }
            }");

            var result = rawResult["__type"].ToObject<ObjectType>();

            result.Fields
                .Single(x => x.Name == "nullable")
                .Type
                .Should().BeEquivalentTo(
                    new ObjectType {
                        Name = "String",
                        Kind = ObjectKinds.Scalar
                    }
                );
        }
    }
}
