using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek
{
    public sealed class OttoServer
    {
        private readonly Schema _schema;
        private readonly IServiceProvider _provider;

        internal OttoServer(Schema schema, IServiceProvider provider)
        {
            _schema = schema;
            _provider = provider;
        }

        public T Execute<T>(string queryText, object inputData = null, bool throwOnError = true)
        {
            return ExecuteAsync<T>(queryText, inputData, throwOnError).GetAwaiter().GetResult();
        }
        
        private async Task<T> ExecuteAsync<T>(string queryText, object inputData = null, bool throwOnError = true)
        {
            var inputs = Inputs.Empty;
            var serializer = _provider.GetRequiredService<IGraphQLSerializer>();
            if(inputData != null)
            {
                var inputsAsJson = JsonConvert.SerializeObject(inputData);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(inputsAsJson));
                inputs = await serializer.ReadAsync<Inputs>(stream);
            }
            var executer = _provider.GetRequiredService<IDocumentExecuter>();
            var opts = new ExecutionOptions
            {
                Query = queryText,
                Schema = _schema,
                RequestServices = _provider,
                Variables = inputs,
            };
            opts.Listeners.AddRange(_provider.GetServices<IDocumentExecutionListener>());
            var resultAsync = executer.ExecuteAsync(opts);
            var executionResult = resultAsync.Result;

            if(executionResult.Errors != null && executionResult.Errors.Count > 0 && throwOnError)
            {
                throw new InvalidOperationException("Errors found: " + JArray.FromObject(executionResult.Errors).ToString());
            }
            
            var memoryStream = new MemoryStream();
            await serializer.WriteAsync(memoryStream, executionResult);
            
            var data = JObject.Parse(System.Text.Encoding.UTF8.GetString(memoryStream.ToArray()))["data"];
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
