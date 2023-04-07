using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OttoTheGeek.TypeModel;
using Xunit;

namespace OttoTheGeek.Tests.TypeModel;

public sealed class OttoScalarTypeMapTests
{
    private enum Flavor
    {
        Grape,
        Chocolate,
        Garlic
    }
    private sealed class Model
    {
        public int Inty { get; set; }
        public IEnumerable<int> Inties { get; set; }
        public Model Child { get; set; }
        public IEnumerable<Model> Chillens { get; set; }
        public Flavor Flav { get; set; }
        public IEnumerable<Flavor> Ingredients { get; set; }
    }

    [Theory]
    [InlineData(nameof(Model.Inty), true)]
    [InlineData(nameof(Model.Inties), true)]
    [InlineData(nameof(Model.Child), false)]
    [InlineData(nameof(Model.Chillens), false)]
    [InlineData(nameof(Model.Flav), true)]
    [InlineData(nameof(Model.Ingredients), true)]
    public void IsScalarOrEnumerable(string name, bool expected)
    {
        var testee = OttoScalarTypeMap.Default;

        var prop = typeof(Model).GetProperty(name);

        testee.IsScalarOrEnumerableOfScalar(prop.PropertyType)
            .Should().Be(expected);
    }
}
