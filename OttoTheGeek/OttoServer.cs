using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

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
        
        internal OttoServer(IServiceProvider provider)
        {
            _provider = provider;
            _schema = provider.GetRequiredService<Schema>();
        }

        public async Task<string> ExecuteAsync(string queryText, object inputData = null, bool throwOnError = true)
        {
            var inputs = Inputs.Empty;
            var serializer = _provider.GetRequiredService<IGraphQLSerializer>();
            if(inputData != null)
            {
                var inputsAsJson = JsonSerializer.Serialize(inputData);
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

            if(executionResult.Errors != null && executionResult.Errors.Count > 0)
            {
                if (throwOnError)
                {
                    throw new InvalidOperationException("Errors found: " + JsonSerializer.Serialize(executionResult.Errors));
                }
            }
            
            var memoryStream = new MemoryStream();
            await serializer.WriteAsync(memoryStream, executionResult);
            
            var jsonResult = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());

            if (!throwOnError)
            {
                var errorStream = new MemoryStream();
                await serializer.WriteAsync(errorStream, executionResult.Errors);
                var jsonErrors = System.Text.Encoding.UTF8.GetString(errorStream.ToArray());

                return $"{{ \"data\": {jsonResult}, \"errors\": {jsonErrors} }}";
            }
            
            return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        }

    }
}
