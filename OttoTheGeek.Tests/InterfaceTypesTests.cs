using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class InterfaceTypesTests
    {
        public sealed class Query
        {
            public IChild Child { get; set; }
        }
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
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.QueryField(x => x.Child)
                    .ResolvesVia<ChildResolver>()
                    .GraphType<ChildObject>(b => b.Interface<IChild>());
            }
        }

        public sealed class ChildResolver : IScalarFieldResolver<IChild>
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
            new Action(() => model.CreateServer())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage("Unable to resolve property Child on class Query");
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
