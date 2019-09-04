using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OttoTheGeek.Core.Tests
{
    public sealed class GraphTypeBuilder_ConfigureScalarQueryField
    {
        public sealed class Model
        {
            public string StringVal { get; set; }
        }

        public sealed class ModelResolver : IQueryFieldResolver<Model>
        {
            public Task<Model> Resolve()
            {
                return Task.FromResult(new Model { StringVal = "hello world" });
            }
        }

        public sealed class Query
        {
            public Model Child { get; }
        }

        [Fact]
        public void ThrowsForUnconfigured()
        {
            var testee = new GraphTypeBuilder<Model>();

            var prop = typeof(Query).GetProperties().Single();

            new Action(() => testee.ConfigureScalarQueryField(prop, new ObjectGraphType(), new ServiceCollection()))
                .Should()
                .Throw<UnableToResolveException>();
        }

        [Fact]
        public void SetsUpField()
        {
            var testee = new GraphTypeBuilder<Model>()
                .WithScalarQueryFieldResolver<ModelResolver>();

            var prop = typeof(Query).GetProperties().Single();
            var graphType = new ObjectGraphType();

            testee.ConfigureScalarQueryField(prop, graphType, new ServiceCollection());

            graphType.Fields.Single().Should().BeEquivalentTo(new {
                Name = prop.Name,
                Type = typeof(Model),
                ResolvedType = testee.BuildGraphType(),
            });
        }

        [Fact]
        public async Task ConfiguresResolver()
        {
            var testee = new GraphTypeBuilder<Model>()
                .WithScalarQueryFieldResolver<ModelResolver>();

            var prop = typeof(Query).GetProperties().Single();
            var graphType = new ObjectGraphType();

            var services = new ServiceCollection();
            testee.ConfigureScalarQueryField(prop, graphType, services);

            var provider = services.BuildServiceProvider();

            var result = await (Task<Model>)graphType.Fields.Single().Resolver.Resolve(new ResolveFieldContext {
                Schema = new Schema {
                    DependencyResolver = new FuncDependencyResolver((t) => provider.GetRequiredService(t))
                }
            });

            result.Should().BeEquivalentTo(await new ModelResolver().Resolve());
        }

    }
}