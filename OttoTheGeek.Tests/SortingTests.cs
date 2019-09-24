using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class SortingTests
    {
        public sealed class Query
        {
            public IEnumerable<Child> Children { get; set; }
        }

        public sealed class Child
        {
            public string Prop1 { get; set; }
            public int Prop2 { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder
                    .ListQueryField(x => x.Children)
                    .WithArgs<Args>()
                    .ResolvesVia<ChildResolver>();

            }
        }

        public sealed class Args
        {
            public OrderValue<Child> OrderBy { get; set; }
        }

        public sealed class ChildResolver : IListFieldWithArgsResolver<Child, Args>
        {
            public static IEnumerable<Child> Data = new[] {
                new Child
                {
                    Prop1 = "z",
                    Prop2 = 1
                },
                new Child
                {
                    Prop1 = "a",
                    Prop2 = 3
                },
                new Child
                {
                    Prop1 = "c",
                    Prop2 = 2
                },
                new Child
                {
                    Prop1 = "b",
                    Prop2 = 5
                },
                new Child
                {
                    Prop1 = "x",
                    Prop2 = 4
                },

            };
            public async Task<IEnumerable<Child>> Resolve(Args args)
            {
                await Task.CompletedTask;

                return args.OrderBy?.ApplyOrdering(Data) ?? Data;
            }
        }

        [Fact]
        public void ReadsPropertyDefinitions()
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
                                enumValues {
                                    name
                                    description
                                }
                            }
                        }
                    }
                }
            }");

            var expectedEnumType = new ObjectType {
                Name = "ChildOrderBy",
                Kind = ObjectKinds.Enum,
                EnumValues = new [] {
                    new EnumValue
                    {
                        Name = $"{nameof(Child.Prop1).ToCamelCase()}_ASC",
                        Description = $"Order by {nameof(Child.Prop1).ToCamelCase()} ascending"
                    },
                    new EnumValue
                    {
                        Name = $"{nameof(Child.Prop1).ToCamelCase()}_DESC",
                        Description = $"Order by {nameof(Child.Prop1).ToCamelCase()} descending"
                    },
                    new EnumValue
                    {
                        Name = $"{nameof(Child.Prop2).ToCamelCase()}_ASC",
                        Description = $"Order by {nameof(Child.Prop2).ToCamelCase()} ascending"
                    },
                    new EnumValue
                    {
                        Name = $"{nameof(Child.Prop2).ToCamelCase()}_DESC",
                        Description = $"Order by {nameof(Child.Prop2).ToCamelCase()} descending"
                    },
                }
            };
            var expectedType = new ObjectType
            {
                Fields = new [] {
                    new ObjectField {
                        Name = "children",
                        Args = new[] {
                            new FieldArgument {
                                Name = "orderBy",
                                Type = expectedEnumType
                            }

                        }
                    }
                }

            };

            var result = rawResult["__type"].ToObject<ObjectType>();

            result.Should().BeEquivalentTo(expectedType);
            var enumType = result.Fields.Single().Args.Single().Type;
            result.Fields.Single().Args.Single().Type.Should().BeEquivalentTo(expectedEnumType);
        }

        public static IEnumerable<object[]> OrderingTestData() => new object[][] {
            new object[] { "prop1_ASC",  ChildResolver.Data.OrderBy(x => x.Prop1) },
            new object[] { "prop1_DESC", ChildResolver.Data.OrderByDescending(x => x.Prop1) },
            new object[] { "prop2_ASC",  ChildResolver.Data.OrderBy(x => x.Prop2) },
            new object[] { "prop2_DESC", ChildResolver.Data.OrderByDescending(x => x.Prop2) },
            new object[] { null, ChildResolver.Data },
        };

        [Theory]
        [MemberData(nameof(OrderingTestData))]
        public void OrdersByProp(string orderingVal, IEnumerable<Child> expected)
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"query($orderBy: ChildOrderBy){
                children(orderBy: $orderBy) {
                    prop1
                    prop2
                }
            }", new { orderBy = orderingVal });

            var result = rawResult["children"].ToObject<Child[]>();
            result.Should().BeEquivalentTo(expected, x => x.WithStrictOrdering());
        }

    }
}