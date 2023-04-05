using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class DescriptionAttributeTests
    {
        private const string ChildPropertyDescription = "Description of " + nameof(Query.Child) + " property on class " + nameof(Query);
        private const string ChildArg1Description = "Description of " + nameof(ChildArgs.Arg1) + " on class " + nameof(ChildArgs);
        public const string ChildObjectClassDescription = "Description for " + nameof(ChildObject) + " type";
        public sealed class Query
        {
            [Description(ChildPropertyDescription)]
            public ChildObject Child { get; set; }
        }

        public sealed class ChildArgs
        {
            [Description(ChildArg1Description)]
            public int Arg1 { get; set; }
        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Query>(b =>
                    b.LooseScalarField(x => x.Child)
                        .WithArgs<ChildArgs>()
                        .ResolvesVia<ChildResolver>()
                );
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldWithArgsResolver<ChildObject, ChildArgs>
        {
            public Task<ChildObject> Resolve(ChildArgs args)
            {
                return Task.FromResult(new ChildObject());
            }
        }

        [Description(ChildObjectClassDescription)]
        public sealed class ChildObject
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;
        }

        [Fact]
        public async Task ReadsDescriptionForField()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Query"") {
                    fields {
                        name
                        description
                    }
                }
            }");

            var expectedField = new ObjectField
            {
                Name = "child",
                Description = ChildPropertyDescription
            };

            var fieldType = rawResult["__type"]["fields"][0].ToObject<ObjectField>();

            fieldType.Should().BeEquivalentTo(expectedField);
        }

        [Fact]
        public async Task ReadsDescriptionForFieldArgument()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Query"") {
                    fields {
                        args {
                            name
                            description
                        }
                    }
                }
            }");

            var expectedField = new FieldArgument
            {
                Name = "arg1",
                Description = ChildArg1Description
            };

            var fieldType = rawResult["__type"]["fields"][0]["args"][0].ToObject<FieldArgument>();

            fieldType.Should().BeEquivalentTo(expectedField);
        }

        [Fact]
        public async Task ReadsDescriptionForClass()
        {
            var server = new Model().CreateServer();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""ChildObject"") {
                    description
                }
            }");

            var actual = rawResult["__type"]["description"].Value<string>();

            actual.Should().Be(ChildObjectClassDescription);
        }
    }
}
