using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class SimpleMutationTests
    {
        public sealed class Mutation
        {
            public ChildObject Child { get; set; }
        }
        public sealed class ChildObject
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;
        }

        public class Model : OttoModel<object, Mutation>
        {

        }

        public sealed class WorkingModel : Model
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Mutation>(b =>
                    b.LooseScalarField(x => x.Child)
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

            var result = server.Execute<JObject>(@"mutation {
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
