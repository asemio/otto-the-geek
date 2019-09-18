using System;
using System.Collections.Generic;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace OttoTheGeek
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

        public ObjectGraphType<T> Resolve<T>(IServiceCollection services)
        {
            return (ObjectGraphType<T>)Resolve(typeof(T), services);
        }

        public IGraphType Resolve(Type modelType, IServiceCollection services)
        {
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

        public bool TryPrime<T>(ObjectGraphType<T> graphType)
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