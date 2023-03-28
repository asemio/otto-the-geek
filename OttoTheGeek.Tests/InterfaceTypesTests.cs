using System;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class InterfaceTypesTests
    {
        public sealed class Query : SimpleScalarQueryModel<IChild> {}
        public interface IChild
        {
            string Value1 { get; }
        }
        public sealed class ChildObject : IChild
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;
        }

        public class Model : OttoModel<Query>
        {

        }

        public sealed class WorkingModel : Model
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<Query>(b =>
                        b.LooseScalarField(x => x.Child)
                            .ResolvesVia<ChildResolver>()
                    )
                    .GraphType<ChildObject>(b => b.Interface<IChild>());
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldResolver<IChild>
        {
            public async Task<IChild> Resolve()
            {
                await Task.CompletedTask;
                return new ChildObject();
            }
        }


        [Fact]
        public void ThrowsUnableToResolveForChildProp()
        {
            var model = new Model();
            new Action(() => model.CreateServer2())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage($"Unable to resolve property Child on class Query");
        }

        [Fact]
        public async Task BuildsSchemaType()
        {
            var server = new WorkingModel().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Query"") {
                    name
                    kind
                    fields {
                        name
                        type {
                            name
                            kind
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
                        Name = "child",
                        Type = new ObjectType
                        {
                            Name = "IChild",
                            Kind = ObjectKinds.Interface
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
            var expectedData = JObject.Parse(@"{
                child: {
                    value1: ""hello"",
                    value2: ""world"",
                    value3: 654,
                }
            }");

            var server = new WorkingModel().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    value1
                    ... on ChildObject {
                        value2
                        value3
                    }
                }
            }");

            result.Should().BeEquivalentTo(expectedData);
        }

    }
}
