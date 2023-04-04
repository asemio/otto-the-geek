using System;
using System.Threading.Tasks;
using FluentAssertions;
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
                ).GraphType<ChildObject>(BuildChildObject);
            }

            private GraphTypeBuilder<ChildObject> BuildChildObject(GraphTypeBuilder<ChildObject> builder)
            {
                return ConfigureViaBaseType(builder)
                    .IgnoreProperty(x => x.Ignored);
            }

            private GraphTypeBuilder<T> ConfigureViaBaseType<T>(GraphTypeBuilder<T> builder) where T : class, IChildObjectBase
            {
                return builder.IgnoreProperty(x => x.IgnoredViaBase);
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldResolver<ChildObject>
        {
            public Task<ChildObject> Resolve()
            {
                return Task.FromResult(new ChildObject());
            }
        }

        public interface IChildObjectBase
        {
            string IgnoredViaBase { get; set; }
        }
        public class ChildObjectBase : IChildObjectBase
        {
            public int Ignored { get; set; }
            public string IgnoredViaBase { get; set; }
        }
        public sealed class ChildObject : ChildObjectBase
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;

            public sealed class Irrelevant
            {
                public static string DoesNotMatter { get; set; }
            }

            public static Irrelevant ThingThatOttoShouldNotCareAbout { get; set; }
        }

        [Fact]
        public void ThrowsUnableToResolveForChildProp()
        {
            var model = new Model();
            new Action(() => model.CreateServer2())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage($"Unable to resolve property Child on class {typeof(SimpleScalarQueryModel<ChildObject>).Name}");
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
        public async Task BuildsChildObjectType()
        {
            var server = new WorkingModel().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""ChildObject"") {
                    name
                    kind
                    fields {
                        name
                    }
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Object,
                Name = "ChildObject",
                Fields = new [] {
                    new ObjectField
                    {
                        Name = "value1"
                    },
                    new ObjectField
                    {
                        Name = "value2"
                    },
                    new ObjectField
                    {
                        Name = "value3"
                    },
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
                    value2
                    value3
                }
            }");

            result.Should().BeEquivalentTo(expectedData);
        }

    }
}
