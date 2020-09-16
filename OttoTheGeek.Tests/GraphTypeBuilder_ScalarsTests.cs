using System;
using System.Linq;
using FluentAssertions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;
using Xunit;

namespace OttoTheGeek.Tests
{

    public sealed class GraphTypeBuilder_ScalarsTests
    {
        public sealed class Model
        {
            public string StringVal { get; set; }
            public int IntVal { get; set; }
            public long LongVal { get; set; }
            public long? NullableLongVal { get; set; }
            public ExampleEnum EnumVal { get; set; }
            public ExampleEnum? NullableEnumVal { get; set; }
        }

        public enum ExampleEnum
        {
            Value1,
            Value2,
            Value3
        }

        private static readonly ComplexGraphType<Model> GraphType = new GraphTypeBuilder<Model>(new Internal.ScalarTypeMap()).BuildGraphType(new Internal.GraphTypeCache(new ScalarTypeMap()), new ServiceCollection());

        [Fact]
        public void BuildsStringField()
        {
            GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.StringVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(NonNullGraphType<StringGraphType>),
                });
        }

        [Fact]
        public void BuildsIntField()
        {
            GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.IntVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(NonNullGraphType<IntGraphType>),
                });
        }

        [Fact]
        public void BuildsLongField()
        {
            GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.LongVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(NonNullGraphType<IntGraphType>),
                });
        }

        [Fact]
        public void BuildsNullableLongField()
        {
            GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.NullableLongVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(IntGraphType),
                });
        }

        [Fact]
        public void BuildsEnumField()
        {
            var fieldDefinition = GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.EnumVal));

            var enumType = fieldDefinition.Type.GetGenericArguments().First();
            enumType.Should().BeAssignableTo<EnumerationGraphType>();
            var enumGraphType = (EnumerationGraphType)Activator.CreateInstance(enumType);

            enumGraphType.Name.Should().Be(nameof(ExampleEnum));
            enumGraphType.Values.Should().BeEquivalentTo(
                new [] {
                    new EnumValueDefinition { Name = nameof(ExampleEnum.Value1), Value = ExampleEnum.Value1 },
                    new EnumValueDefinition { Name = nameof(ExampleEnum.Value2), Value = ExampleEnum.Value2 },
                    new EnumValueDefinition { Name = nameof(ExampleEnum.Value3), Value = ExampleEnum.Value3 },
                }
            );
        }

        [Fact]
        public void BuildsNullableEnumField()
        {
            var fieldDefinition = GraphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.NullableEnumVal));

            fieldDefinition.Type.Should().Be<OttoEnumGraphType<ExampleEnum>>();
        }

        [Fact]
        public void OverrideToId()
        {
            var map = new Internal.ScalarTypeMap();
            var graphType =  new GraphTypeBuilder<Model>(map)
                .ScalarField(x => x.StringVal)
                    .AsGraphType<IdGraphType>()
                .BuildGraphType(new Internal.GraphTypeCache(map), new ServiceCollection());

            graphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.StringVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(IdGraphType),
                });
        }
    }
}