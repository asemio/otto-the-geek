using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests.Integration
{
    public sealed class MutationTests
    {
        public sealed class Args
        {
            public Child Data { get; set; }
        }
        public sealed class Child
        {
            public string Value1 { get; set; } = "unset";
            public string Value2 { get; set; } = "unset";
            public int Value3 { get; set; }
        }

        public class Model : OttoModel<EmptyQueryType, SimpleScalarQueryModel<Child>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<Child>>(b =>
                    b.LooseScalarField(x => x.Child)
                        .WithArgs<Args>()
                        .ResolvesVia<ChildResolver>()
                ).GraphType<EmptyQueryType>(x => x.LooseScalarField(f => f.Dummy).Preloaded());
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldWithArgsResolver<Child, Args>
        {
            public Task<Child> Resolve(Args args)
            {
                return Task.FromResult(new Child {
                    Value1 = args.Data.Value1,
                    Value2 = args.Data.Value2,
                    Value3 = args.Data.Value3
                });
            }
        }

        [Fact]
        public async Task ConfiguresInputType()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""ChildInput"") {
                    name
                    kind
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.InputObject,
                Name = "ChildInput",
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }


        [Fact]
        public async Task ReturnsObjectValues()
        {
            var expectedData = JObject.Parse(@"{
                child: {
                    value1: ""hello"",
                    value2: ""world"",
                    value3: 654,
                }
            }");

            var server = new Model().CreateServer();

            var result = await server.GetResultAsync<JObject>(@"mutation($data: ChildInput!) {
                child(data: $data) {
                    value1
                    value2
                    value3
                }
            }", variables: new {
                data = new {
                    value1 = "hello",
                    value2 = "world",
                    value3 = 654,
                }
            });

            result.Should().BeEquivalentTo(expectedData);
        }
    }
}
