using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class CustomNullabilityTests
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
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder
                    .ListQueryField(x => x.Children)
                    .WithArgs<Args>()
                    .ResolvesVia<ChildResolver>()
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

        public sealed class ChildResolver : IListFieldWithArgsResolver<Child, Args>
        {
            public static IEnumerable<Child> Data = new[] {
                new Child(),
                new Child(),
                new Child(),
                new Child(),
                new Child(),
            };
            public async Task<IEnumerable<Child>> Resolve(Args args)
            {
                await Task.CompletedTask;

                return args.OrderBy?.ApplyOrdering(Data) ?? Data;
            }
        }

        [Fact]
        public void InputTypes()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
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

            var expectedEnumType = ObjectType.NonNullableOf(new ObjectType {
                Name = "ChildOrderBy",
            });

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
        public void OutputTypes()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
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