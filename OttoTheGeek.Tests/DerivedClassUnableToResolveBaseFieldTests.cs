using System;
using FluentAssertions;
using Xunit;

namespace OttoTheGeek.Tests
{

    public sealed class DerivedClassUnableToResolveBaseFieldTests
    {
        public class Query : SimpleScalarQueryModel<ChildObject> {}

        public class Model : OttoModel<Query>
        {

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
                .WithMessage($"Unable to resolve property Child on class Query");
        }

    }
}
