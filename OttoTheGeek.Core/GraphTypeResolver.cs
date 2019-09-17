using System;
using System.Collections.Generic;
using GraphQL.Types;

namespace OttoTheGeek.Core
{
    public sealed class GraphTypeCache
    {
        private readonly Dictionary<Type, IGraphTypeBuilder> _builders;
        private readonly Dictionary<Type, IGraphType> _cache = new Dictionary<Type, IGraphType>();
        public GraphTypeCache() : this(new Dictionary<Type, IGraphTypeBuilder>())
        {

        }
        public GraphTypeCache(Dictionary<Type, IGraphTypeBuilder> builders)
        {
            _builders = builders;
        }
        public ObjectGraphType<T> Resolve<T>()
            where T : class
        {
            if(_cache.TryGetValue(typeof(T), out var cached))
            {
                return (ObjectGraphType<T>)cached;
            }

            if(_builders.TryGetValue(typeof(T), out var cachedBuilder))
            {
                _cache[typeof(T)] = ((GraphTypeBuilder<T>)cachedBuilder).BuildGraphType();
            }
            else {
                _cache[typeof(T)] = new GraphTypeBuilder<T>().BuildGraphType();
            }

            return (ObjectGraphType<T>)_cache[typeof(T)];
        }

        public IGraphType Resolve(Type modelType)
        {
            if(_cache.TryGetValue(modelType, out var cached))
            {
                return cached;
            }

            if(_builders.TryGetValue(modelType, out var cachedBuilder))
            {
                _cache[modelType] = ((dynamic)cachedBuilder).BuildGraphType();
            }
            else {
                dynamic builder = Activator.CreateInstance(typeof(GraphTypeBuilder<>).MakeGenericType(modelType));
                _cache[modelType] = builder.BuildGraphType();
            }

            return _cache[modelType];
        }
    }
}