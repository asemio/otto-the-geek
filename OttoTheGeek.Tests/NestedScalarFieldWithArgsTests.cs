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
    public sealed class NestedScalarFieldWithArgsTests
    {
        public sealed class ChildObject
        {
            public long Id { get; set; }
            public GrandchildObject Child { get; set; }
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
                        b.ScalarField(x => x.Child)
                            .WithArgs<GrandchildArgs>()
                            .ResolvesVia<GrandchildResolver>()
                    )
                    .GraphType<SimpleEnumerableQueryModel<ChildObject>>(b =>
                        b.LooseListField(x => x.Children)
                            .ResolvesVia<ChildrenResolver>()
                    );
            }

            [Obsolete]
            public override OttoServer CreateServer(Action<IServiceCollection> configurator = null)
            {
                return base.CreateServer(x => x.AddSingleton(this));
            }
            
            public override OttoServer CreateServer2(Action<IServiceCollection> configurator = null)
            {
                return base.CreateServer2(x => x.AddSingleton(this));
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

        public sealed class GrandchildResolver : IScalarFieldWithArgsResolver<ChildObject, GrandchildObject, GrandchildArgs>
        {
            private readonly Model _model;

            public GrandchildResolver(Model model)
            {
                _model = model;
            }
            public async Task<IDictionary<object, GrandchildObject>> GetData(IEnumerable<object> keys, GrandchildArgs args)
            {
                _model.IncrementGrandchildResolves();
                await Task.CompletedTask;

                return keys
                    .Cast<long>()
                    .Select(x => (x, new GrandchildObject { Arg = args.Arg, }))
                    .ToDictionary(x => (object)x.Item1, x => x.Item2);
            }

            public object GetKey(ChildObject context)
            {
                return context.Id;
            }
        }


        [Fact]
        public async Task GeneratesSchema()
        {
            var server = new Model().CreateServer2();

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
        public async Task ReturnsData()
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                children {
                    id
                    child(arg: ""derp"") {
                        arg
                    }
                }
            }");

            var actual = rawResult["children"]
                .Select(x => x["child"])
                .Select(x => x.ToObject<GrandchildObject>())
                .ToArray();
            var expected = new[] {
                new GrandchildObject { Arg = "derp" },
                new GrandchildObject { Arg = "derp" },
            };

            actual
                .Should()
                .BeEquivalentTo(expected);
        }
    }
}
