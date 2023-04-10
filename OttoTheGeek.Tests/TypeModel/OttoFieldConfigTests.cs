using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL.Types;
using OttoTheGeek.Internal;
using OttoTheGeek.Internal.ResolverConfiguration;
using OttoTheGeek.TypeModel;
using Xunit;

namespace OttoTheGeek.Tests.TypeModel;

public sealed class OttoFieldConfigTests
{
    private sealed class Outer
    {
        public Inner Inner { get; set; }
        public string Required { get; set; }
        
        public IEnumerable<int> ScalarList { get; set; }
    }
    private sealed class Inner
    {
        public string A { get; set; }
        public string B { get; set; }
    }
    private sealed class ScalarListResolver : ILooseListFieldWithArgsResolver<int, Inner>
    {
        public Task<IEnumerable<int>> Resolve(Inner args)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void ConstructsUnconfiguredInputObject()
    {
        var testee = OttoFieldConfig.ForProperty(typeof(Outer).GetProperty(nameof(Outer.Inner)), typeof(Outer));

        var result = testee.ToGqlNetQueryArgument(OttoSchemaConfig.Empty(typeof(object), typeof(object)), new Dictionary<Type, IInputObjectGraphType>());

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void NullableFieldArgumentType()
    {
        var testee = OttoFieldConfig.ForProperty(typeof(Outer).GetProperty(nameof(Outer.Inner)), typeof(Outer))
            with { Nullability = Nullability.Nullable };

        var result = testee.ToGqlNetQueryArgument(OttoSchemaConfig.Empty(typeof(object), typeof(object)), new Dictionary<Type, IInputObjectGraphType>());

        result.ResolvedType.Should().NotBeNull();
        result.ResolvedType.Should().BeAssignableTo<InputObjectGraphType>();
    }
    
    [Fact]
    public void StringNonNullByDefault()
    {
        var testee = OttoFieldConfig.ForProperty(typeof(Outer).GetProperty(nameof(Outer.Required)), typeof(Outer));

        var result = testee.ToGqlNetQueryArgument(OttoSchemaConfig.Empty(typeof(object), typeof(object)), new Dictionary<Type, IInputObjectGraphType>());

        result.Type.Should().Be(typeof(NonNullGraphType<StringGraphType>));
    }
    
    [Fact]
    public void ArgsForScalarListNonNull()
    {
        var testee = OttoFieldConfig.ForProperty(typeof(Outer).GetProperty(nameof(Outer.ScalarList)), typeof(Outer))
            with {
            ArgumentsType = typeof(Inner),
            ResolverConfiguration = new LooseListWithArgsResolverConfiguration<ScalarListResolver, int, Inner>()
        };

        var result = testee.ToGqlNetField(OttoSchemaConfig.Empty(typeof(object), typeof(object)), new Dictionary<Type, IComplexGraphType>(), new Dictionary<Type, IInputObjectGraphType>());

        result.Arguments.Should().NotBeNullOrEmpty();
    }
}
