﻿using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
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

        public class Model : OttoModel<object, SimpleScalarQueryModel<Child>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<SimpleScalarQueryModel<Child>>(b =>
                    b.LooseScalarField(x => x.Child)
                        .WithArgs<Args>()
                        .ResolvesVia<ChildResolver>()
                );
            }
        }

        public sealed class ChildResolver : IScalarFieldWithArgsResolver<Child, Args>
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
        public void ConfiguresInputType()
        {
            var server = new Model().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
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
        public void ReturnsObjectValues()
        {
            var expectedData = JObject.Parse(@"{
                child: {
                    value1: ""hello"",
                    value2: ""world"",
                    value3: 654,
                }
            }");

            var server = new Model().CreateServer();

            var result = server.Execute<JObject>(@"mutation($data: ChildInput!) {
                child(data: $data) {
                    value1
                    value2
                    value3
                }
            }", new {
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
