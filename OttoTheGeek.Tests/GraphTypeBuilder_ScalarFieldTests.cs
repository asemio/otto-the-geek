using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OttoTheGeek.Tests
{
    public sealed class GraphTypeBuilder_ScalarFieldTests
    {
        public sealed class Model
        {
            public ChildModel1 Child1 { get; set; }
            public ChildModel1 Child1a { get; set; }
            public ChildModel2 Child2 { get; set; }
        }

        public sealed class ChildModel1
        {
            public int Prop { get; set; }

            public sealed class Resolver : IScalarFieldResolver<Model, ChildModel1>
            {
                public Task<Dictionary<object, ChildModel1>> GetData(IEnumerable<object> keys)
                {
                    throw new System.NotImplementedException();
                }

                public object GetKey(Model context)
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        public sealed class ChildModel2
        {
            public int Prop { get; set; }

            public sealed class Resolver : IScalarFieldResolver<Model, ChildModel2>
            {
                public Task<Dictionary<object, ChildModel2>> GetData(IEnumerable<object> keys)
                {
                    throw new System.NotImplementedException();
                }

                public object GetKey(Model context)
                {
                    throw new System.NotImplementedException();
                }
            }
        }

        [Fact]
        public void ThrowsForUnspecifiedPropSameType()
        {
            var map = new Internal.ScalarTypeMap();
            var builder = new GraphTypeBuilder<Model>(map)
                .ScalarField(x => x.Child1)
                .ResolvesVia<ChildModel1.Resolver>();

            new Action(() => builder.BuildGraphType(new Internal.GraphTypeCache(map), new ServiceCollection()))
                .Should()
                .Throw<UnableToResolveException>()
                .WithMessage("Unable to resolve property Child1a on class Model");
        }

        [Fact]
        public void BuildsSchema()
        {
            var map = new Internal.ScalarTypeMap();
            var builder = new GraphTypeBuilder<Model>(map)
                .ScalarField(x => x.Child1)
                .ResolvesVia<ChildModel1.Resolver>()
                .ScalarField(x => x.Child1a)
                .ResolvesVia<ChildModel1.Resolver>()
                .ScalarField(x => x.Child2)
                .ResolvesVia<ChildModel2.Resolver>();

            builder.BuildGraphType(new Internal.GraphTypeCache(map), new ServiceCollection()).Should().NotBeNull();
        }
    }
}