using System.Collections.Generic;

namespace OttoTheGeek.Internal
{
    public static class DictionaryExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> CopyAndAdd<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            var newDict = new Dictionary<TKey, TValue>();
            foreach(var kvp in dict)
            {
                newDict[kvp.Key] = kvp.Value;
            }

            newDict[key] = value;

            return newDict;
        }
    }
}