using System;
using FluentAssertions;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class GraphTypeNameValidationTests
    {
        public class Model : OttoModel<Query>
        {
            protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
            {
                return builder.GraphType<Query>(b =>
                    b
                        .LooseScalarField(x => x.Thing1).Preloaded()
                        .LooseScalarField(x => x.Thing2).Preloaded()
                );
            }
        }
        public sealed class Thing {}

        public sealed class Query
        {
            public sealed class Thing {}

            public Query.Thing Thing1 { get; } = new Query.Thing();
            public GraphTypeNameValidationTests.Thing Thing2 { get; } = new GraphTypeNameValidationTests.Thing();
        }

        [Fact]
        public void ThrowsValidationError()
        {
            new Action(() => new Model().CreateServer())
                .Should()
                .Throw<DuplicateTypeNameException>()
                .And
                .Message
                .Should().ContainAll(
                    "The following C# types have the same GraphQL type name of \"Thing\" configured",
                    "    OttoTheGeek.Tests.GraphTypeNameValidationTests+Query+Thing",
                    "    OttoTheGeek.Tests.GraphTypeNameValidationTests+Thing",
                    "Please configure unique type names for each."
                );
        }
    }
}
