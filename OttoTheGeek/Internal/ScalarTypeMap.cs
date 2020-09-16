using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace OttoTheGeek.Internal
{
    public sealed class ScalarTypeMap
    {
        private Dictionary<Type, Type> _customMappings = new Dictionary<Type, Type>();
        public bool TryGetGraphType(Type type, out Type graphType)
        {
            return _customMappings.TryGetValue(type, out graphType)
                || TryGetDefaultGraphType(type, out graphType);
        }
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
        private static bool TryGetDefaultGraphType(Type t, out Type graphType)
        {
            if(CSharpToGraphqlTypeMapping.TryGetValue(t, out graphType))
            {
                return true;
            }

            return TryGetEnumType(t, out graphType);
        }

        private static bool TryGetEnumType (Type propType, out Type type) {
            type = null;
            if (propType.IsEnum) {
                type = typeof (NonNullGraphType<>).MakeGenericType (
                    typeof (OttoEnumGraphType<>).MakeGenericType (propType)
                );
                return true;
            }

            if (!propType.IsConstructedGenericType) {
                return false;
            }

            if (propType.GetGenericTypeDefinition () != typeof (Nullable<>)) {
                return false;
            }

            var innerType = propType.GetGenericArguments ().Single ();

            if (!innerType.IsEnum) {
                return false;
            }

            type = typeof (OttoEnumGraphType<>).MakeGenericType (innerType);
            return true;
        }
    }
}