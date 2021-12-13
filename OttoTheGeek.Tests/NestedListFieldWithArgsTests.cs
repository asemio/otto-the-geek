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

namespace OttoTheGeek.Tests
{
    public sealed class NestedListFieldWithArgsTests
    {
        public sealed class ChildObject
        {
            public long Id { get; set; }
            public IEnumerable<GrandchildObject> Children { get; set; }
        }
        public sealed class GrandchildObject
        {
            public string Arg { get; set; }
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
                            .WithArgs<GrandchildArgs>()
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

        public sealed class GrandchildArgs
        {
            public string Arg { get; set; }
        }

        public sealed class GrandchildResolver : IListFieldWithArgsResolver<ChildObject, GrandchildObject, GrandchildArgs>
        {
            private readonly Model _model;

            public GrandchildResolver(Model model)
            {
                _model = model;
            }
            public async Task<ILookup<object, GrandchildObject>> GetData(IEnumerable<object> keys, GrandchildArgs args)
            {
                _model.IncrementGrandchildResolves();
                await Task.CompletedTask;

                return keys
                    .Cast<long>()
                    .SelectMany(x => new[]{
                        new GrandchildObject { Arg = args.Arg, },
                        new GrandchildObject { Arg = args.Arg, },
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
                    children(arg: ""derp"") {
                        arg
                    }
                }
            }");

            var actual = rawResult["children"]
                .SelectMany(x => x["children"])
                .Select(x => x.ToObject<GrandchildObject>())
                .ToArray();
            var expected = new[] {
                new GrandchildObject { Arg = "derp" },
                new GrandchildObject { Arg = "derp" },
                new GrandchildObject { Arg = "derp" },
                new GrandchildObject { Arg = "derp" },
            };

            actual
                .Should()
                .BeEquivalentTo(expected);
        }

        [Fact]
        public void AvoidsNPlusOne()
        {
            var model = new Model();
            var server = model.CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    id
                    children(arg: ""derp"") {
                        arg
                    }
                }
            }");

            model.GrandchildResolves.Should().Be(1);
        }

    }
}
