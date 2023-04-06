using System;
using GraphQL.Types;
using OttoTheGeek.Internal;
using OttoTheGeek.TypeModel;

namespace OttoTheGeek
{

    public sealed class SchemaBuilder
    {
        private readonly Type _schemaType;
        internal readonly OttoSchemaConfig _schemaConfig;

        internal SchemaBuilder(Type schemaType) : this(schemaType, OttoSchemaConfig.Empty(schemaType.GetGenericArguments()[0], schemaType.GetGenericArguments()[1]))
        {
        }
        private SchemaBuilder(Type schemaType, OttoSchemaConfig schemaConfig)
        {
            _schemaType = schemaType;
            _schemaConfig = schemaConfig;
        }

        public SchemaBuilder GraphType<TType>(Func<GraphTypeBuilder<TType>, GraphTypeBuilder<TType>> configurator)
            where TType : class
        {
            var (self, builder) = configurator(GetGraphTypeBuilder<TType>())
                .RunSchemaBuilderCallbacks(this);

            return new SchemaBuilder(self._schemaType, self._schemaConfig.UpdateLegacyBuilder(builder));
        }

        public SchemaBuilder ScalarType<TScalar, TConverter>()
            where TConverter : ScalarTypeConverter<TScalar>, new()
        {
            var newConfig = _schemaConfig;
            if(typeof(TScalar).IsValueType)
            {
                _schemaConfig.LegacyScalars.AddGraphType(typeof(TScalar), typeof(NonNullGraphType<CustomScalarGraphType<TScalar, TConverter>>));
                var nullableType = typeof(Nullable<>).MakeGenericType(typeof(TScalar));
                _schemaConfig.LegacyScalars.AddGraphType(nullableType, typeof(CustomScalarGraphType<TScalar, TConverter>));
                newConfig = _schemaConfig
                    .AddScalarType(typeof(TScalar), typeof(CustomScalarGraphType<TScalar, TConverter>))
                    .AddScalarType(nullableType, typeof(CustomScalarGraphType<TScalar, TConverter>));
            }
            else
            {
                _schemaConfig.LegacyScalars.AddGraphType(typeof(TScalar), typeof(CustomScalarGraphType<TScalar, TConverter>));
                newConfig = _schemaConfig.AddScalarType(typeof(TScalar), typeof(CustomScalarGraphType<TScalar, TConverter>));
            }

            return new SchemaBuilder(_schemaType, newConfig);
        }

        private GraphTypeBuilder<TType> GetGraphTypeBuilder<TType>()
            where TType : class
        {
            _schemaConfig.LegacyBuilders.TryGetValue(typeof(TType), out var untypedBuilder);
            var builder =
                ((GraphTypeBuilder<TType>)untypedBuilder)
                ?? new GraphTypeBuilder<TType>();

            return builder;
        }
    }
}
