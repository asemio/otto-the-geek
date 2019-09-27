using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public static class ScalarTypeMap
    {
        private static readonly IReadOnlyDictionary<Type, Type> CSharpToGraphqlTypeMapping = new Dictionary<Type, Type>{
            [typeof(string)]            = typeof(NonNullGraphType<StringGraphType>),
            [typeof(int)]               = typeof(NonNullGraphType<IntGraphType>),
            [typeof(long)]              = typeof(NonNullGraphType<IntGraphType>),
            [typeof(double)]            = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(float)]             = typeof(NonNullGraphType<FloatGraphType>),
            [typeof(decimal)]           = typeof(NonNullGraphType<DecimalGraphType>),
            [typeof(bool)]              = typeof(NonNullGraphType<BooleanGraphType>),
            [typeof(DateTime)]          = typeof(NonNullGraphType<DateGraphType>),
            [typeof(DateTimeOffset)]    = typeof(NonNullGraphType<DateTimeOffsetGraphType>),
            [typeof(Guid)]              = typeof(NonNullGraphType<IdGraphType>),
            [typeof(short)]             = typeof(NonNullGraphType<ShortGraphType>),
            [typeof(ushort)]            = typeof(NonNullGraphType<UShortGraphType>),
            [typeof(ulong)]             = typeof(NonNullGraphType<ULongGraphType>),
            [typeof(uint)]              = typeof(NonNullGraphType<UIntGraphType>),
            [typeof(TimeSpan)]          = typeof(NonNullGraphType<TimeSpanGraphType>),

            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(int?)]              = typeof(IntGraphType),
            [typeof(long?)]             = typeof(IntGraphType),
            [typeof(double?)]           = typeof(FloatGraphType),
            [typeof(float?)]            = typeof(FloatGraphType),
            [typeof(decimal?)]          = typeof(DecimalGraphType),
            [typeof(bool?)]             = typeof(BooleanGraphType),
            [typeof(DateTime?)]         = typeof(DateGraphType),
            [typeof(DateTimeOffset?)]   = typeof(DateTimeOffsetGraphType),
            [typeof(Guid?)]             = typeof(IdGraphType),
            [typeof(short?)]            = typeof(ShortGraphType),
            [typeof(ushort?)]           = typeof(UShortGraphType),
            [typeof(ulong?)]            = typeof(ULongGraphType),
            [typeof(uint?)]             = typeof(UIntGraphType),
            [typeof(TimeSpan?)]         = typeof(TimeSpanGraphType),
        };
        public static bool TryGetGraphType(Type t, out Type graphType)
        {
            return CSharpToGraphqlTypeMapping.TryGetValue(t, out graphType);
        }
    }
}