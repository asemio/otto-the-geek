using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests.Integration
{
    public sealed class NestedListFieldTests
    {
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
        public class Model : OttoModel<SimpleEnumerableQueryModel<ChildObject>>
        {
            private int _grandchildResolves;

            public int GrandchildResolves => _grandchildResolves;

            public void IncrementGrandchildResolves()
            {
                Interlocked.Increment(ref _grandchildResolves);
            }
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<ChildObject>(b =>
                        b.ListField(x => x.Children)
                            .ResolvesVia<GrandchildResolver>()
                    )
                    .GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                        b.LooseListField(x => x.Children)
                            .ResolvesVia<ChildrenResolver>()
                    );
            }

            public override OttoServer CreateServer(Action<IServiceCollection> configurator = null)
            {
                return base.CreateServer(x => x.AddSingleton(this));
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

        public sealed class GrandchildResolver : IListFieldResolver<ChildObject, GrandchildObject>
        {
            private readonly Model _model;

            public GrandchildResolver(Model model)
            {
                _model = model;
            }
            public async Task<ILookup<object, GrandchildObject>> GetData(IEnumerable<object> keys)
            {
                _model.IncrementGrandchildResolves();
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
        public async Task GeneratesSchema()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
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
        public async Task ReturnsData()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
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

        [Fact]
        public async Task AvoidsNPlusOne()
        {
            var model = new Model();
            var server = model.CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                children {
                    id
                    children {
                        value1
                        value2
                        value3
                    }
                }
            }");

            model.GrandchildResolves.Should().Be(1);
        }

    }
}
