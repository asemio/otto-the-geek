using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class CustomScalarTypeTests
    {
        public class Model : OttoModel<SimpleScalarQueryModel<ChildObject>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder
                    .ScalarType<FancyInt, FancyIntConverter>()
                    .ScalarType<FancyString, FancyStringConverter>()
                    .GraphType<SimpleScalarQueryModel<ChildObject>>(b =>
                        b.Named("Query")
                            .LooseScalarField(x => x.Child)
                                .WithArgs<Args>()
                                .ResolvesVia<ChildResolver>()
                );
            }
        }

        public sealed class ChildResolver : ILooseScalarFieldWithArgsResolver<ChildObject, Args>
        {
            public async Task<ChildObject> Resolve(Args args)
            {
                await Task.CompletedTask;

                var ret = new ChildObject();

                ret.IntValue = args.IntValue ?? ret.IntValue;
                ret.StrValue = args.StrValue ?? ret.StrValue;

                return ret;
            }
        }

        public sealed class Args
        {
            public FancyInt? IntValue { get; set; }
            public FancyString? StrValue { get; set; }
        }

        public sealed class ChildObject
        {
            public FancyInt IntValue { get; set; } = FancyInt.FromInt(654);

            public FancyInt? NeverValue { get; set; }
            public FancyString StrValue { get; set; } = FancyString.FromString("foo");
        }

        public struct FancyInt
        {
            public int Value { get; }
            private FancyInt(int value)
            {
                Value = value;
            }
            public static FancyInt FromInt(int value)
            {
                return new FancyInt(value);
            }
        }

        public struct FancyString
        {
            public string Value { get; }
            private FancyString(string value)
            {
                Value = value;
            }
            public static FancyString FromString(string value)
            {
                return new FancyString(value);
            }
        }

        public sealed class FancyIntConverter : ScalarTypeConverter<FancyInt>
        {
            public override string Convert(FancyInt value)
            {
                return $"**{value.Value}**";
            }

            public override FancyInt Parse(string value)
            {
                var trimmed = value.Trim('*');
                if(!int.TryParse(trimmed, out var intVal))
                {
                    throw new System.Exception($"couldn't parse {trimmed} as an int");
                }
                return FancyInt.FromInt(intVal);
            }
        }
        public sealed class FancyStringConverter : ScalarTypeConverter<FancyString>
        {
            public override string Convert(FancyString value)
            {
                return $"--{value.Value}--";
            }

            public override FancyString Parse(string value)
            {
                var trimmed = value.Trim('-');
                return FancyString.FromString(trimmed);
            }
        }


        [Fact]
        public async Task NamesCustomType()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                __type(name: ""FancyInt"") {
                    name
                    kind
                }
            }");

            result["__type"].ToObject<ObjectType>().Should().BeEquivalentTo(new ObjectType {
                Name = "FancyInt",
                Kind = ObjectKinds.Scalar
            });
        }
        
        [Fact]
        public async Task HandlesNullability()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                __type(name: ""Query"") {
                    fields {
                        args {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }");

            result["__type"].ToObject<ObjectType>().Fields.Single().Args.Should().BeEquivalentTo(
                new FieldArgument
                {
                    Name = "intValue",
                    Type = new ObjectType
                    {
                        Name = "FancyInt",
                        Kind = ObjectKinds.Scalar
                    }
                },
                new FieldArgument
                {
                    Name = "strValue",
                    Type = new ObjectType
                    {
                        Name = "FancyString",
                        Kind = ObjectKinds.Scalar
                    }
                }
            );
        }

        [Fact]
        public async Task ResolvesFields()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                child {
                    intValue
                    strValue
                    neverValue
                }
            }");

            result["child"].Value<string>("intValue").Should().Be("**654**");
            result["child"].Value<string>("strValue").Should().Be("--foo--");
            result["child"].Value<string>("neverValue").Should().BeNull();
        }

        [Fact]
        public async Task ParsesFieldArgumentAsString()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"query ($intValue: FancyInt!){
                child(intValue: $intValue) {
                    intValue
                    strValue
                }
            }", variables: new {
                intValue = "**234**"
            });

            result["child"].Value<string>("intValue").Should().Be("**234**");
        }

        [Fact]
        public async Task ParsesStringFieldArgument()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                child(strValue: ""--wat--"") {
                    intValue
                    strValue
                }
            }");

            result["child"].Value<string>("strValue").Should().Be("--wat--");
        }

        [Fact]
        public async Task ParsesFieldArgumentAsInt()
        {
            var server = new Model().CreateServer2();

            var result = await server.GetResultAsync<JObject>(@"{
                child(intValue: 234) {
                    intValue
                    strValue
                }
            }");

            result["child"].Value<string>("intValue").Should().Be("**234**");
        }
    }
}
