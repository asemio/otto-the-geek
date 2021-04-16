using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class ComputedAndDefaultPropTests
    {

        public sealed class Query
        {
            public Child DefaultChild { get; set; } = new Child();
            public Child ComputedChild => new Child();
        }

        public sealed class Child
        {
            public int AnInt => 42;
            public string AString => "Hello World!";
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .GraphType<Query>(b =>
                        b
                            .LooseScalarField(x => x.DefaultChild).Preloaded()
                            .LooseScalarField(x => x.ComputedChild).Preloaded()
                );
            }
        }

        [Fact]
        public void ResolvesDefaultPropertyValue()
        {
            var server = new Model().CreateServer();

            var result = server.Execute<string>(@"{
                defaultChild {
                    anInt
                    aString
                }
            }");

            result.Should().Be(JObject.Parse(@"{
                ""defaultChild"": {
                    ""anInt"": 42,
                    ""aString"": ""Hello World!""
                }
            }").ToString());

        }

        [Fact]
        public void ResolvesComputedPropertyValue()
        {
            var server = new Model().CreateServer();

            var result = server.Execute<string>(@"{
                computedChild {
                    anInt
                    aString
                }
            }");

            result.Should().Be(JObject.Parse(@"{
                ""computedChild"": {
                    ""anInt"": 42,
                    ""aString"": ""Hello World!""
                }
            }").ToString());

        }

    }
}