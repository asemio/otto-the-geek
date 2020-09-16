using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class NestedScalarFieldTests
    {
        public class Model : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                    b.LooseListField(x => x.Children)
                        .ResolvesVia<ChildrenResolver>());
            }
        }

        public class WorkingModel : Model
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                var configured = builder
                    .GraphType<ChildObject>(ConfigureChildObject)
                    .GraphType<GrandchildObject>(ConfigureGrandchild);

                return base.ConfigureSchema(configured);
            }

            private GraphTypeBuilder<ChildObject> ConfigureChildObject(GraphTypeBuilder<ChildObject> builder)
            {
                return builder.ScalarField(x => x.Child)
                    .ResolvesVia<GrandchildResolver>();
            }

            protected virtual GraphTypeBuilder<GrandchildObject> ConfigureGrandchild(GraphTypeBuilder<GrandchildObject> builder)
            {
                return builder.IgnoreProperty(x => x.CircularRelationship);
            }
        }

        public class DeepNestedWorkingModel : WorkingModel
        {
            protected override GraphTypeBuilder<GrandchildObject> ConfigureGrandchild(GraphTypeBuilder<GrandchildObject> builder)
            {
                return builder.ScalarField(x => x.CircularRelationship)
                    .ResolvesVia<ChildFromGrandchildResolver>();
            }
        }

        public sealed class ChildrenResolver : ILooseListFieldResolver<ChildObject>
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

        public sealed class ChildFromGrandchildResolver : IScalarFieldResolver<GrandchildObject, ChildObject>
        {
            public async Task<Dictionary<object, ChildObject>> GetData(IEnumerable<object> keys)
            {
                await Task.CompletedTask;
                return keys
                    .Cast<int>()
                    .ToDictionary(x => (object)x, x => new ChildObject { Id = x });
            }

            public object GetKey(GrandchildObject context)
            {
                return context.Value3;
            }
        }

        public sealed class GrandchildResolver : IScalarFieldResolver<ChildObject, GrandchildObject>
        {
            public static Dictionary<object, GrandchildObject> Data => new Dictionary<object, GrandchildObject>{
                { 1L, new GrandchildObject {
                    Value1 = "one",
                    Value2 = "uno",
                    Value3 = 10000
                }},
                { 2L, new GrandchildObject {
                    Value1 = "two",
                    Value2 = "dos",
                    Value3 = 20000
                }}
            };

            public Task<Dictionary<object, GrandchildObject>> GetData(IEnumerable<object> keys)
            {
                return Task.FromResult(Data);
            }

            public object GetKey(ChildObject context)
            {
                return context.Id;
            }
        }

        public sealed class ChildObject
        {
            public long Id { get; set; }
            public GrandchildObject Child { get; set; }
        }
        public sealed class GrandchildObject
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public int Value3 { get; set; }
            public ChildObject CircularRelationship { get; set; }
        }

        [Fact]
        public void ThrowsUnableToResolveForChildProp()
        {
            var model = new Model();
            new Action(() => model.CreateServer())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage("Unable to resolve property Child on class ChildObject");
        }

        [Fact]
        public void GeneratesSchema()
        {
            var server = new WorkingModel().CreateServer();

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
                Name = "child",
                Type = new ObjectType {
                    Name = "GrandchildObject",
                    Kind = ObjectKinds.Object
                }
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Fields
                .SingleOrDefault(x => x.Name == "child")
                .Should()
                .BeEquivalentTo(expectedField);
        }

        [Fact]
        public void ReturnsData()
        {
            var server = new WorkingModel().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    id
                    child {
                        value1
                        value2
                        value3
                    }
                }
            }");

            var actual = rawResult["children"]
                .Select(x => x["child"])
                .Select(x => x.ToObject<GrandchildObject>())
                .ToArray();

            actual
                .Should()
                .BeEquivalentTo(GrandchildResolver.Data.Select(x => x.Value));
        }

        [Fact]
        public void ReturnsDeeplyNestedData()
        {
            var server = new DeepNestedWorkingModel().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    id
                    child {
                        value1
                        value2
                        value3
                        circularRelationship {
                            id
                            child {
                                value1
                                value2
                                value3
                            }
                        }
                    }
                }
            }");

            var actual = rawResult["children"]
                .Select(x => x["child"])
                .Select(x => x["circularRelationship"])
                .Select(x => x.ToObject<ChildObject>())
                .ToArray();

            var expected = GrandchildResolver.Data.Values
                .Select(x => new ChildObject { Id = x.Value3 });

            actual
                .Should()
                .BeEquivalentTo(expected);
        }
    }

}