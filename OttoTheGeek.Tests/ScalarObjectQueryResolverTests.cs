using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class ScalarObjectQueryResolverTests
    {
        public class Model : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {

        }

        public sealed class WorkingModel : Model
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                    b.Named("Query").LooseScalarField(x => x.Child)
                        .ResolvesVia<ChildResolver>()
                );
            }
        }

        public sealed class ChildResolver : IScalarFieldResolver<ChildObject>
        {
            public Task<ChildObject> Resolve()
            {
                return Task.FromResult(new ChildObject());
            }
        }

        public sealed class ChildObject
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;
        }

        [Fact]
        public void ThrowsUnableToResolveForChildProp()
        {
            var model = new Model();
            new Action(() => model.CreateServer())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage($"Unable to resolve property Child on class {typeof(SimpleScalarQueryModel<ChildObject>).Name}");
        }

        [Fact]
        public void BuildsSchemaType()
        {
            var server = new WorkingModel().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                __type(name:""Query"") {
                    name
                    kind
                    fields {
                        name
                    }
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Object,
                Name = "Query",
                Fields = new [] {
                    new ObjectField
                    {
                        Name = "child"
                    }
                }
            };

            var queryType = rawResult["__type"].ToObject<ObjectType>();

            queryType.Should().BeEquivalentTo(expectedType);
        }

        [Fact]
        public void ReturnsObjectValues()
        {
            var expectedData = JObject.Parse(@"{
                child: {
                    value1: ""hello"",
                    value2: ""world"",
                    value3: 654,
                }
            }");

            var server = new WorkingModel().CreateServer();

            var result = server.Execute<JObject>(@"{
                child {
                    value1
                    value2
                    value3
                }
            }");

            result.Should().BeEquivalentTo(expectedData);
        }

    }
}