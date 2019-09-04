using System;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek.Core
{
    public sealed class OttoServer
    {
        private Schema _schema;
        private readonly IServiceProvider _provider;

        internal OttoServer(Schema schema, IServiceProvider provider)
        {
            _schema = schema;
            _provider = provider;
        }

        public T Execute<T>(string queryText)
        {
            var executer = _provider.GetRequiredService<IDocumentExecuter>();
            var opts = new ExecutionOptions
            {
                Query = queryText,
                Schema = _schema,
            };
            var resultAsync = executer.ExecuteAsync(opts);
            var executionResult = resultAsync.Result;

            if(executionResult.Errors != null && executionResult.Errors.Count > 0)
            {
                throw new InvalidOperationException("Errors found: " + JArray.FromObject(executionResult.Errors).ToString());
            }

            var data = JObject.FromObject(executionResult.Data);

            if(typeof(T) == typeof(string)) {
                return (T)(object)(data.ToString());
            }

            return data.ToObject<T>();
        }
    }
}