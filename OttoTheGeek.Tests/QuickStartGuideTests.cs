using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class QuickStartGuideTests
    {

        public sealed class Query
        {
            public Child Child { get; set; }
        }

        public sealed class Child
        {
            public int AnInt { get; set; }
            public string AString { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.QueryField(x => x.Child)
                    .ResolvesVia<Resolver>();
            }
        }

        public sealed class Resolver : IScalarFieldResolver<Child>
        {
            public Task<Child> Resolve()
            {
                return Task.FromResult(new Child {
                    AnInt = 42,
                    AString = "Hello World!"
                });
            }
        }

        [Fact]
        public void QsgRuns()
        {
            var server = new Model().CreateServer();

            var result = server.Execute<string>(@"{
                child {
                    anInt
                    aString
                }
            }");

            result.Should().Be(JObject.Parse(@"{
                ""child"": {
                    ""anInt"": 42,
                    ""aString"": ""Hello World!""
                }
            }").ToString());

        }

    }
}