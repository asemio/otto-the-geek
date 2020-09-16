/// This file is borrowed and modified from https://github.com/graphql-dotnet/server/blob/4837d785840efff34d283788d3ed87e855d03b4a/src/Transports.AspNetCore/GraphQLHttpMiddleware.cs
///
/// Modification is necessary because the middleware as written did synchronous reads which are disallowed by default in ASP.Net Core 3.0 and beyond.
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OttoTheGeek.GraphQLNetHacks
{
    public class Middleware<TSchema>
        where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";
        private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly PathString _path;

        public Middleware(ILogger<GraphQLHttpMiddleware<TSchema>> logger, RequestDelegate next, PathString path)
        {
            _logger = logger;
            _next = next;
            _path = path;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest || !context.Request.Path.StartsWithSegments(_path))
            {
                await _next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            var httpRequest = context.Request;
            var gqlRequest = new GraphQLRequest();

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();

            if (HttpMethods.IsGet(httpRequest.Method) || (HttpMethods.IsPost(httpRequest.Method) && httpRequest.Query.ContainsKey(GraphQLRequest.QueryKey)))
            {
                ExtractGraphQLRequestFromQueryString(httpRequest.Query, gqlRequest);
            }
            else if (HttpMethods.IsPost(httpRequest.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.");
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JsonContentType:
                        gqlRequest = await Deserialize(httpRequest.Body);
                        break;
                    case GraphQLContentType:
                        gqlRequest.Query = await ReadAsStringAsync(httpRequest.Body);
                        break;
                    case FormUrlEncodedContentType:
                        var formCollection = await httpRequest.ReadFormAsync();
                        ExtractGraphQLRequestFromPostBody(formCollection, gqlRequest);
                        break;
                    default:
                        await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}', '{GraphQLContentType}', or '{FormUrlEncodedContentType}'. See: http://graphql.org/learn/serving-over-http/.");
                        return;
                }
            }

            object userContext = null;
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();

            if (userContextBuilder != null)
            {
                userContext = await userContextBuilder.BuildUserContext(context);
            }

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();

            var result = await executer.ExecuteAsync(
                gqlRequest.OperationName,
                gqlRequest.Query,
                gqlRequest.GetInputs(),
                userContext,
                context.RequestAborted);

            if (result.Errors != null)
            {
                _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
            }

            await WriteResponseAsync(context, writer, result);
        }

        private Task WriteBadRequestResponseAsync(HttpContext context, IDocumentWriter writer, string errorMessage)
        {
            var result = new ExecutionResult()
            {
                Errors = new ExecutionErrors()
                {
                    new ExecutionError(errorMessage)
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400; // Bad Request

            return writer.WriteAsync(context.Response.Body, result);
        }

        private Task WriteResponseAsync(HttpContext context, IDocumentWriter writer, ExecutionResult result)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200; // OK

            return writer.WriteAsync(context.Response.Body, result);
        }

        private static async Task<GraphQLRequest> Deserialize(Stream s)
        {
            using (var reader = new StreamReader(s))
            {
                var jsonStr = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<GraphQLRequest>(jsonStr);
            }
        }

        private static async Task<string> ReadAsStringAsync(Stream s)
        {
            using (var reader = new StreamReader(s))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static void ExtractGraphQLRequestFromQueryString(IQueryCollection qs, GraphQLRequest gqlRequest)
        {
            gqlRequest.Query = qs.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = qs.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null;
            gqlRequest.OperationName = qs.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null;
        }

        private static void ExtractGraphQLRequestFromPostBody(IFormCollection fc, GraphQLRequest gqlRequest)
        {
            gqlRequest.Query = fc.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = fc.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValue) ? JObject.Parse(variablesValue[0]) : null;
            gqlRequest.OperationName = fc.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null;
        }
    }
}