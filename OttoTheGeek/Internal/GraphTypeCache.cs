using System;
using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek.Internal
{
    public sealed class GraphTypeCache
    {
        private readonly Dictionary<Type, IGraphTypeBuilder> _builders;
        private readonly Dictionary<Type, IGraphType> _cache = new Dictionary<Type, IGraphType>();
        private readonly Dictionary<Type, QueryArguments> _argsCache = new Dictionary<Type, QueryArguments>();

        public GraphTypeCache() : this(new Dictionary<Type, IGraphTypeBuilder>())
        {

        }
        public GraphTypeCache(Dictionary<Type, IGraphTypeBuilder> builders)
        {
            _builders = builders;
        }

        public ComplexGraphType<T> GetOrCreate<T>(IServiceCollection services)
        {
            return (ComplexGraphType<T>)GetOrCreate(typeof(T), services);
        }

        public IGraphType GetOrCreate(Type modelType, IServiceCollection services)
        {
            if(ScalarTypeMap.TryGetGraphType(modelType, out var scalarGraphType))
            {
                return (IGraphType)Activator.CreateInstance(scalarGraphType);
            }
            if(_cache.TryGetValue(modelType, out var cached))
            {
                return cached;
            }

            if(_builders.TryGetValue(modelType, out var cachedBuilder))
            {
                _cache[modelType] = ((dynamic)cachedBuilder).BuildGraphType(this, services);
            }
            else {
                dynamic builder = Activator.CreateInstance(typeof(GraphTypeBuilder<>).MakeGenericType(modelType));
                _cache[modelType] = builder.BuildGraphType(cache: this, services: services);
            }

            return _cache[modelType];
        }

        public QueryArguments GetOrCreateArguments<T>(IServiceCollection services)
        {
            var modelType = typeof(T);
            if(_argsCache.TryGetValue(modelType, out var args))
            {
                return args;
            }

            if(_builders.TryGetValue(modelType, out var cachedBuilder))
            {
                _argsCache[modelType] = ((dynamic)cachedBuilder).BuildQueryArguments(this, services);
            }
            else {
                dynamic builder = Activator.CreateInstance(typeof(GraphTypeBuilder<>).MakeGenericType(modelType));
                _argsCache[modelType] = builder.BuildQueryArguments(cache: this, services: services);
            }

            return _argsCache[modelType];
        }

        public bool TryPrime<T>(ComplexGraphType<T> graphType)
        {
            if(_cache.ContainsKey(typeof(T)))
            {
                return false;
            }

            _cache[typeof(T)] = graphType;

            return true;
        }
    }
}