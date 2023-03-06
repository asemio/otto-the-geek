using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek.Tests
{
    public static class StringAssertionExtensions
    {
        public static async Task<T> GetResultAsync<T>(
            this OttoServer server,
            string queryText,
            string path = "",
            object variables = null,
            bool throwOnError = true)
        {
            var json = await server.ExecuteAsync(queryText, variables, throwOnError);

            path = "data." + path;

            var jsonElem = JObject.Parse(json);

            var pathParts = path.Split(".")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            foreach (var part in pathParts)
            {
                if (!jsonElem.TryGetValue(part, out var newElem))
                {
                    throw new ArgumentException($"Unable to resolve {part} from {json}");
                }

                jsonElem = (JObject)newElem;
            }

            if (typeof(T) == typeof(JObject))
            {
                return (T) (object) jsonElem;
            }

            return jsonElem.ToObject<T>();
        }

        public static T Execute<T>(
            this OttoServer server,
            string queryText,
            object inputData = null,
            bool throwOnError = true)
        {
            throw new NotImplementedException();
        }
    }
}
