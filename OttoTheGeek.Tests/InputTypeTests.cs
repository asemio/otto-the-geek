using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Newtonsoft.Json.Linq;
using OttoTheGeek.RuntimeSchema;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class InputTypeTests
    {
        public enum Texture
        {
            [Description("Desc of Crunchy")]
            Crunchy,
            Smooth,
            Chunky,
            Grainy
        }

        public sealed class Child
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
            public IEnumerable<int> ListOfInts { get; set; }
            public IEnumerable<Texture> ListOfTextures { get; set; }
            public ComplexThing ComplexThing { get; set; }

            public IEnumerable<ComplexThing> MoreThings { get; set; }
        }

        public sealed class Args
        {
            public int AnInt { get; set; }
            public int? ANullableInt { get; set; }
            public string IgnoredString { get; set; }
            public Texture? Texture { get; set; }
            public IEnumerable<int> ListOfInts { get; set; }
            public IEnumerable<Texture> ListOfTextures { get; set; }
            public ComplexThing ComplexThing { get; set; } = new ComplexThing();
            public IEnumerable<ComplexThing> MoreThings { get; set; } = new[] { new ComplexThing() };
        }

        public sealed class ComplexThing
        {
            public int NumericThing { get; set; } = 22;
            public string StringyValue { get; set; } = "Imma nested stringy thing";
        }

        public sealed class Resolver : ILooseScalarFieldWithArgsResolver<Child, Args>
        {
            public async Task<Child> Resolve(Args args)
            {
                await Task.CompletedTask;
                return new Child {
                    AnInt = args.AnInt,
                    ANullableInt = args.ANullableInt,
                    Texture = args.Texture,
                    ListOfInts = args.ListOfInts,
                    ListOfTextures = args.ListOfTextures,
                    ComplexThing = args.ComplexThing,
                    MoreThings = args.MoreThings,
                };
            }
        }

        public sealed class Model : OttoModel<SimpleScalarQueryModel<Child>>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return LoadConfigurators(this.GetType().Assembly, builder);
            }
        }

        public sealed class Configurator
            : IGraphTypeConfigurator<Model, SimpleScalarQueryModel<Child>>
            , IGraphTypeConfigurator<Model, Args>
            , IGraphTypeConfigurator<Model, Child>
        {
            public GraphTypeBuilder<SimpleScalarQueryModel<Child>> Configure(GraphTypeBuilder<SimpleScalarQueryModel<Child>> builder)
            {
                return builder
                    .Named("Query")
                    .LooseScalarField(x => x.Child)
                        .WithArgs<Args>()
                        .ResolvesVia<Resolver>()
                        ;
            }

            public GraphTypeBuilder<Args> Configure(GraphTypeBuilder<Args> builder)
                => builder.IgnoreProperty(x => x.IgnoredString);

            public GraphTypeBuilder<Child> Configure(GraphTypeBuilder<Child> builder)
                => builder
                    .ListField(x => x.ListOfInts).Preloaded()
                    .ListField(x => x.ListOfTextures).Preloaded()
                    .ScalarField(x => x.ComplexThing).Preloaded()
                    .ListField(x => x.MoreThings).Preloaded()
                ;
        }

        [Fact]
        public async Task ConfiguresScalarArgs()
        {
            var queryType = await GetQueryObjectType();

            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.AnInt).ToCamelCase(),
                    Type = ObjectType.NonNullableOf(ObjectType.Int)
                });
            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.ANullableInt).ToCamelCase(),
                    Type = ObjectType.Int
                });
            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.Texture).ToCamelCase(),
                    Type = new ObjectType {
                        Name = "Texture",
                        Kind = ObjectKinds.Enum
                    }
                });
        }

        [Fact]
        public async Task ConfiguresScalarLists()
        {
            var queryType = await GetQueryObjectType();

            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.ListOfInts).ToCamelCase(),
                    Type = ObjectType.ListOf(
                        ObjectType.NonNullableOf(
                            ObjectType.Int))
                });
            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.ListOfTextures).ToCamelCase(),
                    Type = ObjectType.ListOf(
                        ObjectType.NonNullableOf(
                            new ObjectType {Name = "Texture", Kind = ObjectKinds.Enum}))
                });
        }

        [Fact]
        public async Task ConfiguresComplexArgs()
        {
            var queryType = await GetQueryObjectType();

            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.ComplexThing).ToCamelCase(),
                    Type = ObjectType.NonNullableOf(
                        new ObjectType { Name = $"{nameof(ComplexThing)}Input", Kind = ObjectKinds.InputObject })
                });
            queryType.Fields.Single().Args.Should().ContainEquivalentOf(
                new FieldArgument
                {
                    Name = nameof(Args.MoreThings).ToCamelCase(),
                    Type =
                        ObjectType.ListOf(ObjectType.NonNullableOf(
                            new ObjectType { Name = $"{nameof(ComplexThing)}Input", Kind = ObjectKinds.InputObject }))
                });
        }

        private static async Task<ObjectType> GetQueryObjectType()
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<ObjectType>(@"{
                __type(name:""Query"") {
                    name
                    kind
                    fields {
                        name
                        args {
                            name
                            type {
                                name
                                kind
                                ofType {
                                    name
                                    kind
                                    ofType {
                                        name
                                        kind
                                    }
                                }
                            }
                        }
                    }
                }
            }", "__type");

            return rawResult;
        }

        [Fact]
        public async Task ConfiguresEnumValues()
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"{
                __type(name:""Texture"") {
                    name
                    kind
                    enumValues {
                        name
                        description
                    }
                }
            }");

            var expectedType = new ObjectType {
                Kind = ObjectKinds.Enum,
                Name = "Texture",
                EnumValues = new [] {
                    new EnumValue { Name = "Crunchy", Description = "Desc of Crunchy" },
                    new EnumValue { Name = "Smooth" },
                    new EnumValue { Name = "Chunky" },
                    new EnumValue { Name = "Grainy" }
                }
            };

            var fieldType = rawResult["__type"].ToObject<ObjectType>();

            fieldType.Should().BeEquivalentTo(expectedType);
        }

        public static IEnumerable<object[]> DeserializesArgsData()
        {
            yield return new object[] { new Args { AnInt = 4, ANullableInt = null } };

            yield return new object[] { new Args { AnInt = 7, ANullableInt = 20, ListOfInts = new[] { 4, 5, 6, 0 } } };

            yield return new object[] { new Args { AnInt = 4, Texture = Texture.Chunky } };

            yield return new object[] { new Args { AnInt = 4, Texture = Texture.Chunky, ListOfTextures = new[] { Texture.Chunky } } };
        }

        [Theory]
        [MemberData(nameof(DeserializesArgsData))]
        public async Task DeserializesArgs(Args args)
        {
            var server = new Model().CreateServer2();

            var rawResult = await server.GetResultAsync<JObject>(@"
            query ($anInt: Int!, $aNullableInt: Int, $texture: Texture, $listOfInts: [Int!], $listOfTextures: [Texture!], $complexThing: ComplexThingInput!, $moreThings: [ComplexThingInput!]) {
                child(
                        anInt: $anInt,
                        aNullableInt: $aNullableInt,
                        texture: $texture,
                        listOfInts: $listOfInts,
                        listOfTextures: $listOfTextures,
                        complexThing: $complexThing,
                        moreThings: $moreThings
                        ) {
                    anInt
                    aNullableInt
                    texture
                    listOfInts
                    listOfTextures
                    complexThing { numericThing stringyValue }
                    moreThings { numericThing stringyValue }
                }
            }", variables: new {
                anInt = args.AnInt,
                aNullableInt = args.ANullableInt,
                texture = args.Texture?.ToString(),
                listOfInts = args.ListOfInts,
                listOfTextures = args.ListOfTextures?.Select(x => x.ToString()),
                complexThing = new
                {
                    numericThing = args.ComplexThing.NumericThing,
                    stringyValue = args.ComplexThing.StringyValue,
                },
                moreThings = new[]
                {
                    new
                    {
                        numericThing = args.ComplexThing.NumericThing,
                        stringyValue = args.ComplexThing.StringyValue,
                    },
                }
            });

            var result = rawResult["child"].ToObject<Child>();

            result.Should().BeEquivalentTo(args);
        }
    }
}
