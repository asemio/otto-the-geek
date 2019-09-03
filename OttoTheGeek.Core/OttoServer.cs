using System;
using GraphQL;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek.Core
{
    public sealed class OttoServer
    {
        private Schema _schema;

        internal OttoServer(Schema schema)
        {
            _schema = schema;
        }

        public T Execute<T>(string queryText)
        {
            var json = _schema.Execute(_ => _.Query = queryText);
            var result = JObject.Parse(json);

            if(result["errors"] != null)
            {
                throw new InvalidOperationException("Errors found: " + result["errors"].ToString());
            }

            if(typeof(T) == typeof(string)) {
                return (T)(object)(result["data"].ToString());
            }

            return result["data"].ToObject<T>();
        }
    }
}