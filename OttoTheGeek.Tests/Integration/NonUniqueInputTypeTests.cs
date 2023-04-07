using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace OttoTheGeek.Tests.Integration;

public class NonUniqueInputTypeTests
{
    private sealed class Child
    {
        public IEnumerable<DataObject> Vals { get; set; }
    }

    private sealed class DataObject
    {
        public string Val1 { get; set; }
        public string Val2 { get; set; }
    }

    private sealed class Args
    {
        public IEnumerable<DataObject> Vals { get; set; }
    }

    private sealed class Query
    {
        public Child Child1 { get; set; }
        public Child Child2 { get; set; }
    }

    private sealed class ChildResolver : ILooseScalarFieldWithArgsResolver<Child, Args>
    {
        public async Task<Child> Resolve(Args args)
        {
            await Task.CompletedTask;
            return new Child
            {
                Vals = args.Vals,
            };
        }
    }

    private sealed class Model : OttoModel<Query>
    {
        protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
        {
            return builder
                .GraphType<Query>(ConfigureQuery)
                .GraphType<Child>(ConfigureChild)
                ;
        }

        private GraphTypeBuilder<Child> ConfigureChild(GraphTypeBuilder<Child> builder)
        {
            return builder.ListField(x => x.Vals).Preloaded();
        }

        private GraphTypeBuilder<Query> ConfigureQuery(GraphTypeBuilder<Query> builder)
        {
            return builder
                .LooseScalarField(x => x.Child1)
                    .WithArgs<Args>()
                    .ResolvesVia<ChildResolver>()
                .LooseScalarField(x => x.Child2)
                    .WithArgs<Args>()
                    .ResolvesVia<ChildResolver>()
                ;
        }
    }

    [Fact]
    public async Task Queries()
    {
        var server = new Model().CreateServer();

        var result = await server.ExecuteAsync(@"{
            child1(vals: [{ val1: ""v1"", val2: ""v2"" }]) {
                vals { val1 val2 }
            }
        }");

        result.Should().NotBeNull();
    }
}
