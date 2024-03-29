﻿using System;
using System.Collections.Immutable;
using GraphQL.Types;
using OttoTheGeek.Internal;

namespace OttoTheGeek.TypeModel;

public record OttoScalarTypeMap(ImmutableDictionary<Type, Type> Map)
{
    public static OttoScalarTypeMap Default = new (DefaultTypeMap());

    private static ImmutableDictionary<Type, Type> DefaultTypeMap()
    {
        var builder = ImmutableDictionary.CreateBuilder<Type, Type>();
        builder.Add(typeof(string)            , typeof(StringGraphType));
        builder.Add(typeof(int)               , typeof(IntGraphType));
        builder.Add(typeof(long)              , typeof(IntGraphType));
        builder.Add(typeof(double)            , typeof(FloatGraphType));
        builder.Add(typeof(float)             , typeof(FloatGraphType));
        builder.Add(typeof(decimal)           , typeof(DecimalGraphType));
        builder.Add(typeof(bool)              , typeof(BooleanGraphType));
        builder.Add(typeof(DateTime)          , typeof(DateGraphType));
        builder.Add(typeof(DateTimeOffset)    , typeof(DateTimeOffsetGraphType));
        builder.Add(typeof(Guid)              , typeof(IdGraphType));
        builder.Add(typeof(short)             , typeof(ShortGraphType));
        builder.Add(typeof(ushort)            , typeof(UShortGraphType));
        builder.Add(typeof(ulong)             , typeof(ULongGraphType));
        builder.Add(typeof(uint)              , typeof(UIntGraphType));
        builder.Add(typeof(TimeSpan)          , typeof(TimeSpanGraphType));

        builder.Add(typeof(long?)             , typeof(IntGraphType));
        builder.Add(typeof(int?)              , typeof(IntGraphType));
        builder.Add(typeof(double?)           , typeof(FloatGraphType));
        builder.Add(typeof(float?)            , typeof(FloatGraphType));
        builder.Add(typeof(decimal?)          , typeof(DecimalGraphType));
        builder.Add(typeof(bool?)             , typeof(BooleanGraphType));
        builder.Add(typeof(DateTime?)         , typeof(DateGraphType));
        builder.Add(typeof(DateTimeOffset?)   , typeof(DateTimeOffsetGraphType));
        builder.Add(typeof(Guid?)             , typeof(IdGraphType));
        builder.Add(typeof(short?)            , typeof(ShortGraphType));
        builder.Add(typeof(ushort?)           , typeof(UShortGraphType));
        builder.Add(typeof(ulong?)            , typeof(ULongGraphType));
        builder.Add(typeof(uint?)             , typeof(UIntGraphType));
        builder.Add(typeof(TimeSpan?)         , typeof(TimeSpanGraphType));
        
        return builder.ToImmutable();
    }

    public OttoScalarTypeMap Add(Type clrType, Type graphType)
    {
        return this with
        {
            Map = Map.Add(clrType, graphType)
        };
    }

    public bool IsScalarOrEnumerableOfScalar(Type t)
    {
        var coreType = t.GetEnumerableElementType() ?? t;

        if (Map.ContainsKey(coreType))
        {
            return true;
        }

        var unwrapped = coreType.UnwrapNullable();

        return unwrapped.IsEnum;
    }
}
