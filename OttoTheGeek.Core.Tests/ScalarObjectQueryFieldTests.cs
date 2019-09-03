using System;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Core.Tests
{
    public sealed class ScalarObjectQueryResolverTests
    {
        public sealed class Query
        {
            public ChildObject Child { get; set; }
        }

        public class Model : OttoModel<Query>
        {

        }

        public sealed class WorkingModel : Model
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.QueryField(x => x.Child)
                    .ResolvesVia<ChildResolver>();
            }
        }

        public sealed class ChildResolver : IQueryFieldResolver<ChildObject>
        {
            public Task<ChildObject> Resolve()
            {
                return Task.FromResult(new ChildObject());
            }
        }

        public sealed class ChildObject
        {
            public string Value1 => "hello";
            public string Value2 => "world";
            public int Value3 => 654;
        }

        [Fact]
        public void ThrowsUnableToResolveForChildProp()
        {
            var model = new Model();
            new Action(() => model.CreateServer())
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage("Unable to resolve property Child on class Query");
        }

        [Fact]
        public void BuildsServerWhenResolverPresent()
        {
            var model = new WorkingModel();
            new Action(() => model.CreateServer())
                .Should()
                .NotThrow();
        }

    }
}
