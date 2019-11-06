using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class NestedListFieldTests
    {
        public sealed class Query
        {
            public IEnumerable<ChildObject> Children { get; set; }
        }
        public sealed class ChildObject
        {
            public long Id { get; set; }
            public IEnumerable<GrandchildObject> Children { get; set; }
        }
        public sealed class GrandchildObject
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public int Value3 { get; set; }
        }
        public class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<ChildObject>(b =>
                        b.ListField(x => x.Children)
                            .ResolvesVia<GrandchildResolver>()
                    )
                    .GraphType<Query>(b =>
                        b.LooseListField(x => x.Children)
                            .ResolvesVia<ChildrenResolver>()
                    );
            }
        }

        public sealed class ChildrenResolver : IListFieldResolver<ChildObject>
        {
            public async Task<IEnumerable<ChildObject>> Resolve()
            {
                await Task.CompletedTask;

                return new[] {
                    new ChildObject { Id = 1 },
                    new ChildObject { Id = 2 }
                };
            }
        }

        public sealed class GrandchildResolver : IListFieldResolver<ChildObject, GrandchildObject>
        {
            public async Task<ILookup<object, GrandchildObject>> GetData(IEnumerable<object> keys)
            {
                await Task.CompletedTask;

                return keys
                    .Cast<long>()
                    .SelectMany(x => new[]{
                        new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = (int)(1000 * x + 1) },
                        new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = (int)(1000 * x + 2) }
                    }, (key, child) => (key, child))
                    .ToLookup(x => (object)x.Item1, x => x.Item2);
            }

            public object GetKey(ChildObject context)
            {
                return context.Id;
            }
        }


        [Fact]
        public void GeneratesSchema()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                __type(name:""ChildObject"") {
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
            }");

            var expectedField = new ObjectField
            {
                Name = "children",
                Type = ObjectType.ListOf(new ObjectType {
                    Name = "GrandchildObject",
                    Kind = ObjectKinds.Object
                })
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Fields
                .SingleOrDefault(x => x.Name == "children")
                .Should()
                .BeEquivalentTo(expectedField);
        }

        [Fact]
        public void ReturnsData()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    id
                    children {
                        value1
                        value2
                        value3
                    }
                }
            }");

            var actual = rawResult["children"]
                .SelectMany(x => x["children"])
                .Select(x => x.ToObject<GrandchildObject>())
                .ToArray();
            var expected = new[] {
                new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = 1001 },
                new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = 1002 },
                new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = 2001 },
                new GrandchildObject { Value1 = "one", Value2 = "uno", Value3 = 2002 }
            };

            actual
                .Should()
                .BeEquivalentTo(expected);
        }

    }

}