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
            var graphType = new GraphTypeBuilder<Model>().BuildGraphType(new Internal.GraphTypeCache(), new ServiceCollection());

            graphType.Name.Should().Be(nameof(Model));
        }

        [Fact]
        public void CustomName()
        {
            var graphType = new GraphTypeBuilder<Model>()
                .Named("ImmaCustomName")
                .BuildGraphType(new Internal.GraphTypeCache(), new ServiceCollection());

            graphType.Name.Should().Be("ImmaCustomName");
        }
    }
}