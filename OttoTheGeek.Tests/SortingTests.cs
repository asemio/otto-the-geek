using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Newtonsoft.Json.Linq;
using OttoTheGeek.Connections;
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
            public string Irrelevant { get; }
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
                    .GraphType<Args>(ConfigureArgs);
            }

            private GraphTypeBuilder<Args> ConfigureArgs(GraphTypeBuilder<Args> builder)
            {
                return builder.ConfigureOrderBy(
                    x => x.OrderBy,
                    ConfigureOrderBy);
            }

            private OrderByBuilder<Child> ConfigureOrderBy(OrderByBuilder<Child> builder)
            {
                return builder
                    .AddValue("custom", descending: false)
                    .AddValue("custom", descending: true)
                    .Ignore(x => x.Irrelevant)
                    ;
            }
        }

        public sealed class ConnectionModel : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Query>(b =>
                    b.ConnectionField(x => x.Children)
                        .WithArgs<ConnectionArgs>()
                        .ResolvesVia<ChildConnectionResolver>()
                    )
                    .GraphType<ConnectionArgs>(ConfigureArgs);
            }

            private GraphTypeBuilder<ConnectionArgs> ConfigureArgs(GraphTypeBuilder<ConnectionArgs> builder)
            {
                return builder.ConfigureOrderBy(
                    x => x.OrderBy,
                    ConfigureOrderBy);
            }

            private OrderByBuilder<Child> ConfigureOrderBy(OrderByBuilder<Child> builder)
            {
                return builder
                    .AddValue("custom", descending: false)
                    .AddValue("custom", descending: true)
                    .Ignore(x => x.Irrelevant)
                    ;
            }
        }

        public sealed class Args
        {
            public OrderValue<Child> OrderBy { get; set; }
        }
        public sealed class ConnectionArgs : Connections.PagingArgs<Child>
        {
            public string SearchText { get; set; }
        }

        public sealed class ChildResolver : IListFieldWithArgsResolver<Child, Args>
        {
            public static IEnumerable<Child> Data = new[] {
                new Child
                {
                    Prop1 = "z0000000",
                    Prop2 = 1
                },
                new Child
                {
                    Prop1 = "a0000003",
                    Prop2 = 3
                },
                new Child
                {
                    Prop1 = "c0000004",
                    Prop2 = 2
                },
                new Child
                {
                    Prop1 = "b0000001",
                    Prop2 = 5
                },
                new Child
                {
                    Prop1 = "x0000002",
                    Prop2 = 4
                },

            };
            public async Task<IEnumerable<Child>> Resolve(Args args)
            {
                await Task.CompletedTask;

                if(args.OrderBy?.Prop != null)
                {
                    return Data.AsQueryable().OrderBy(args.OrderBy);
                }

                if(args.OrderBy?.Name == "custom")
                {
                    if(args.OrderBy.Descending)
                    {
                        return Data.OrderByDescending(x => new string(x.Prop1.Reverse().ToArray()));
                    }
                    return Data.OrderBy(x => new string(x.Prop1.Reverse().ToArray()));
                }

                return Data;
            }
        }

        public sealed class ChildConnectionResolver : IConnectionResolver<Child, ConnectionArgs>
        {
            public Task<Connection<Child>> Resolve(ConnectionArgs args)
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<object[]> ReadsPropertyDefinitionsData()
        {
            yield return new object[] { new Model().CreateServer(), nameof(Model) };
            yield return new object[] { new ConnectionModel().CreateServer(), nameof(ConnectionModel) };
        }

        [Theory]
        [MemberData(nameof(ReadsPropertyDefinitionsData))]
        public void ReadsPropertyDefinitions(OttoServer server, string name)
        {
            GC.KeepAlive(name);
            var rawResult = server.Execute<JObject>(@"{
                __type(name:""ChildOrderBy"") {
                    name
                    kind
                    enumValues {
                        name
                        description
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
                    new EnumValue
                    {
                        Name = $"custom_ASC",
                        Description = $"Order by custom ascending"
                    },
                    new EnumValue
                    {
                        Name = $"custom_DESC",
                        Description = $"Order by custom descending"
                    },
                }
            };

            var result = rawResult["__type"].ToObject<ObjectType>();

            result.Should().BeEquivalentTo(expectedEnumType);
        }

        public static IEnumerable<object[]> OrderingTestData() => new object[][] {
            new object[] { "prop1_ASC",  ChildResolver.Data.OrderBy(x => x.Prop1) },
            new object[] { "prop1_DESC", ChildResolver.Data.OrderByDescending(x => x.Prop1) },
            new object[] { "prop2_ASC",  ChildResolver.Data.OrderBy(x => x.Prop2) },
            new object[] { "prop2_DESC", ChildResolver.Data.OrderByDescending(x => x.Prop2) },
            new object[] { "custom_ASC", ChildResolver.Data.OrderBy(x => new string(x.Prop1.Reverse().ToArray())) },
            new object[] { "custom_DESC", ChildResolver.Data.OrderByDescending(x => new string(x.Prop1.Reverse().ToArray())) },
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
