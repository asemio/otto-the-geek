using System.Linq;
using FluentAssertions;
using GraphQL.Types;
using Xunit;

namespace OttoTheGeek.Core.Tests
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

        private static readonly ObjectGraphType<Model> GraphType = new GraphTypeBuilder<Model>().BuildGraphType();

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
    }
}