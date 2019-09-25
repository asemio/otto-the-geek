using System.Linq;
using FluentAssertions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
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
        }

        private static readonly ComplexGraphType<Model> GraphType = new GraphTypeBuilder<Model>().BuildGraphType(new Internal.GraphTypeCache(), new ServiceCollection());

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
        public void OverrideToId()
        {
            var graphType =  new GraphTypeBuilder<Model>()
                .ScalarField(x => x.StringVal)
                    .AsGraphType<IdGraphType>()
                .BuildGraphType(new Internal.GraphTypeCache(), new ServiceCollection());

            graphType.Fields
                .SingleOrDefault(x => x.Name == nameof(Model.StringVal))
                .Should()
                .BeEquivalentTo(new {
                    Type = typeof(IdGraphType),
                });
        }
    }
}