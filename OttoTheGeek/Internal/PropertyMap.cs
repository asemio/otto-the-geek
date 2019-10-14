using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OttoTheGeek.Internal
{
    public sealed class PropertyMap<TValue>
    {
        private IEnumerable<(Type, string, TValue)> _values;
        public PropertyMap() : this(new (Type, string, TValue)[0])
        {
        }

        private PropertyMap(IEnumerable<(Type, string, TValue)> values)
        {
            _values = values;
        }


        public PropertyMap<TValue> Add(PropertyInfo key, TValue value)
        {
            var newValues = _values
                .Where(x => !Matches(key, x))
                .Concat(new[] { (key.DeclaringType, key.Name, value) })
                .ToArray();

            return new PropertyMap<TValue>(newValues);
        }

        public TValue Get(PropertyInfo key)
        {
            return _values
                .Where(x => Matches(key, x))
                .Select(x => x.Item3)
                .SingleOrDefault();
        }

        private static bool Matches(PropertyInfo key, (Type, string, TValue) tuple)
        {
            return key.DeclaringType == tuple.Item1
                && key.Name == tuple.Item2;
        }
    }
}