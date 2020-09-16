using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using OttoTheGeek.Internal;

namespace OttoTheGeek
{
    public sealed class SchemaBuilder
    {
        private readonly Type _schemaType;
        private readonly Dictionary<Type, IGraphTypeBuilder> _builders;
        private readonly ScalarTypeMap _scalarTypeMap;

        internal SchemaBuilder(Type schemaType) : this(schemaType, new Dictionary<Type, IGraphTypeBuilder>())
        {
        }
        private SchemaBuilder(Type schemaType, Dictionary<Type, IGraphTypeBuilder> builders)
        {
            _schemaType = schemaType;
            _builders = builders;
            _scalarTypeMap = new ScalarTypeMap();
        }

        public SchemaBuilder GraphType<TType>(Func<GraphTypeBuilder<TType>, GraphTypeBuilder<TType>> configurator)
            where TType : class
        {
            var (self, builder) = configurator(GetGraphTypeBuilder<TType>())
                .RunSchemaBuilderCallbacks(this);

            var dict = new Dictionary<Type, IGraphTypeBuilder>(self._builders);
            dict[typeof(TType)] = builder;

            return new SchemaBuilder(self._schemaType, dict);
        }
        public OttoSchemaInfo Build(IServiceCollection services)
        {
            var cache = new GraphTypeCache(_builders, _scalarTypeMap);
            var queryType = _schemaType.GetGenericArguments().First();
            var mutationType = _schemaType.GetGenericArguments().Skip(1).First();
            var queryGraphType = cache.GetOrCreate(queryType, services);
            var mutationGraphType = cache.GetOrCreate(mutationType, services);

            var otherTypes = _builders.Values
                .Where(x => x.NeedsRegistration)
                .Select(x => x.BuildGraphType(cache, services))
                .ToArray();

            cache.ValidateNoDuplicates();

            var schema = new OttoSchemaInfo((IObjectGraphType)queryGraphType, (IObjectGraphType)mutationGraphType, null, otherTypes);

            return schema;
        }

        private GraphTypeBuilder<TType> GetGraphTypeBuilder<TType>()
            where TType : class
        {
            _builders.TryGetValue(typeof(TType), out var untypedBuilder);
            var builder =
                ((GraphTypeBuilder<TType>)untypedBuilder)
                ?? new GraphTypeBuilder<TType>(_scalarTypeMap);

            return builder;
        }
    }
}