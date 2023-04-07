using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Tests.Integration
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
        public async Task ResolvesDefaultPropertyValue()
        {
            var server = new Model().CreateServer();

            var result = await server.GetResultAsync<JObject>(@"{
                defaultChild {
                    anInt
                    aString
                }
            }");

            result.Should().BeEquivalentTo(new JObject(
                new JProperty("defaultChild", new JObject(
                    new JProperty("anInt",  42),
                    new JProperty("aString", "Hello World!")
                ))
            ));
        }

        [Fact]
        public async Task ResolvesComputedPropertyValue()
        {
            var server = new Model().CreateServer();

            var result = await server.GetResultAsync<JObject>(@"{
                computedChild {
                    anInt
                    aString
                }
            }");

            result.Should().BeEquivalentTo(new JObject(
                new JProperty("computedChild", new JObject(
                    new JProperty("anInt",  42),
                    new JProperty("aString", "Hello World!")
                ))
            ));
        }
    }
}
