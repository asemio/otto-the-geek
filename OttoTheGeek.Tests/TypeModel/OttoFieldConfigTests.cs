﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using GraphQL.Types;
using OttoTheGeek.TypeModel;
using Xunit;

namespace OttoTheGeek.Tests.TypeModel;

public sealed class OttoFieldConfigTests
{
    private sealed class Outer
    {
        public Inner Inner { get; set; }
    }
    private sealed class Inner
    {
        
    }
    [Fact]
    public void ConstructsUnconfiguredInputObject()
    {
        var testee = OttoFieldConfig.ForProperty(typeof(Outer).GetProperty(nameof(Inner)), typeof(Outer));

        var result = testee.ToGqlNetQueryArgument(OttoSchemaConfig.Empty(typeof(object), typeof(object)), new Dictionary<Type, IInputObjectGraphType>());

        result.Should().NotBeNull();
    }
    
}
