using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class PreloadedScalarFieldTests
    {
        public sealed class Query
        {
            public IEnumerable<ChildObject> Children { get; set; }
        }

        public class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder.ListQueryField(x => x.Children)
                    .ResolvesVia<ChildrenResolver>();
            }
        }

        public class WorkingModel : Model
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                var configured = builder
                    .GraphType<ChildObject>(ConfigureChildObject);

                return base.ConfigureSchema(configured);
            }

            private GraphTypeBuilder<ChildObject> ConfigureChildObject(GraphTypeBuilder<ChildObject> builder)
            {
                return builder.ScalarField(x => x.Child)
                    .Preloaded();
            }
        }

        public sealed class ChildrenResolver : IListFieldResolver<ChildObject>
        {
            public static IEnumerable<ChildObject> Data => new[] {
                new ChildObject { Id = 1, Child = new GrandchildObject { Value1 = "one" } },
                new ChildObject { Id = 2, Child = new GrandchildObject { Value1 = "two" } }
            };

            public async Task<IEnumerable<ChildObject>> Resolve()
            {
                await Task.CompletedTask;

                return Data;
            }
        }

        public sealed class ChildObject
        {
            public long Id { get; set; }
            public GrandchildObject Child { get; set; }
        }
        public sealed class GrandchildObject
        {
            public string Value1 { get; set; }
        }


        [Fact]
        public void ReturnsData()
        {
            var server = new WorkingModel().CreateServer();

            var rawResult = server.Execute<JObject>(@"{
                children {
                    id
                    child {
                        value1
                    }
                }
            }");

            var actual = rawResult["children"]
                .ToObject<ChildObject[]>();

            actual
                .Should()
                .BeEquivalentTo(ChildrenResolver.Data);
        }
    }

}