using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class GraphTypeBuilder_NameTests
    {
        public sealed class Model
        {
            public string StringVal { get; set; }
        }

        [Fact]
        public void DefaultName()
        {
            var map = new Internal.ScalarTypeMap();
            var cache = new Internal.GraphTypeCache(map);
            var graphType = new GraphTypeBuilder<Model>(map).BuildGraphType(cache, new ServiceCollection());

            graphType.Name.Should().Be(nameof(Model));
        }

        [Fact]
        public void CustomName()
        {
            var graphType = new GraphTypeBuilder<Model>(new Internal.ScalarTypeMap())
                .Named("ImmaCustomName")
                .BuildGraphType(new Internal.GraphTypeCache(new Internal.ScalarTypeMap()), new ServiceCollection());

            graphType.Name.Should().Be("ImmaCustomName");
        }
    }
}