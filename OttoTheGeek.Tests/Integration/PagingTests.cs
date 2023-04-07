using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.Connections;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests.Integration
{
    public class PagingTests
    {
        public class Model : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                    b.Named("Query").ConnectionField(x => x.Children)
                        .ResolvesVia<ChildrenResolver>()
                )
;
            }
        }

        public sealed class CustomConnectionArgsModel : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                    b.Named("Query").ConnectionField(x => x.Children)
                        .WithArgs<CustomArgs>()
                        .ResolvesVia<CustomChildrenResolver>()
                );
            }
        }

        public sealed class CustomArgs : PagingArgs<ChildObject>
        {
            public string SearchText { get; set; }
        }

        public sealed class ChildrenResolver : IConnectionResolver<ChildObject>
        {
            public async Task<Connection<ChildObject>> Resolve(PagingArgs<ChildObject> args)
            {
                await Task.CompletedTask;

                var offset = args.Offset;
                var count = args.Count;
                return GenerateData(offset, count, null);
            }

            public static Connection<ChildObject> GenerateData(int offset, int count, string searchText)
            {
                return new Connection<ChildObject>
                {
                    Records = Enumerable.Range(offset, count)
                        .Select(x => new ChildObject
                        {
                            Value1 = $"Thing{x}",
                            Value2 = $"Cosa{x}",
                            SearchText = searchText,
                            Value3 = x
                        }),
                    TotalCount = 100
                };
            }
        }

        public sealed class CustomChildrenResolver : IConnectionResolver<ChildObject, CustomArgs>
        {

            public async Task<Connection<ChildObject>> Resolve(CustomArgs args)
            {
                await Task.CompletedTask;

                var offset = args.Offset;
                var count = args.Count;
                return ChildrenResolver.GenerateData(offset, count, args.SearchText);
            }
        }

        public sealed class ChildObject
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string SearchText { get; set; }
            public int Value3 { get; set; }
        }

        [Fact]
        public async Task BuildsSchemaType()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
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
                        type {
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
                        Name = "children",
                        Type = ObjectType.ConnectionOf(
                            new ObjectType {
                                Name = "ChildObject",
                                Kind = ObjectKinds.Object
                            }
                        ),
                        Args = new [] {
                            new FieldArgument {
                                Name = "offset",
                                Type = ObjectType.NonNullableOf(ObjectType.Int)
                            },
                            new FieldArgument {
                                Name = "count",
                                Type = ObjectType.NonNullableOf(ObjectType.Int)
                            },
                            new FieldArgument {
                                Name = "orderBy",
                                Type = new ObjectType {
                                    Name = "ChildObjectOrderBy",
                                    Kind = ObjectKinds.Enum
                                }
                            },
                        }
                    }
                }
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        [Fact]
        public async Task ReturnsObjectValues()
        {
            var server = new Model().CreateServer();

            var result = await server.GetResultAsync<Connection<ChildObject>>(@"{
                children(offset: 22, count: 11) {
                    totalCount
                    records {
                        value1
                        value2
                        value3
                    }
                }
            }", "children");

            result.Should().BeEquivalentTo(ChildrenResolver.GenerateData(22, 11, null));
        }

        [Fact]
        public async Task ReturnsObjectValuesFromCustomArgs()
        {
            var server = new CustomConnectionArgsModel().CreateServer();

            var result = await server.GetResultAsync<Connection<ChildObject>>(@"{
                children(offset: 22, count: 11, searchText: ""derp"") {
                    totalCount
                    records {
                        value1
                        value2
                        value3
                        searchText
                    }
                }
            }", "children");

            result.Should().BeEquivalentTo(ChildrenResolver.GenerateData(22, 11, "derp"));
        }
    }
}
