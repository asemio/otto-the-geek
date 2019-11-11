using System;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek
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

        public T Execute<T>(string queryText, object inputData = null, bool throwOnError = true)
        {
            var inputs = new Inputs();
            if(inputData != null)
            {
                inputs = JsonConvert.SerializeObject(inputData).ToInputs();
            }
            var executer = _provider.GetRequiredService<IDocumentExecuter>();
            var opts = new ExecutionOptions
            {
                Query = queryText,
                Schema = _schema,
                Inputs = inputs
            };
            opts.Listeners.Add(_provider.GetRequiredService<DataLoaderDocumentListener>());
            var resultAsync = executer.ExecuteAsync(opts);
            var executionResult = resultAsync.Result;

            if(executionResult.Errors != null && executionResult.Errors.Count > 0 && throwOnError)
            {
                throw new InvalidOperationException("Errors found: " + JArray.FromObject(executionResult.Errors).ToString());
            }

            var data = JObject.FromObject(executionResult.Data);
            if(!throwOnError)
            {
                data = new JObject(
                    new JProperty("data", data),
                    new JProperty("errors", JArray.FromObject(executionResult.Errors ?? new ExecutionErrors()))
                );
            }

            if(typeof(T) == typeof(string)) {
                return (T)(object)(data.ToString());
            }
            if(typeof(T) == typeof(JObject))
            {
                return (T)(object)data;
            }

            return data.ToObject<T>();
        }
    }
}