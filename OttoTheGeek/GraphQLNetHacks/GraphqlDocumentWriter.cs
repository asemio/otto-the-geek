using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OttoTheGeek.GraphQLNetHacks
{
    // This class overrides the default IDocumentWriter to prevent synchronous writes which are disallowed by default in ASP.Net Core 3.0 and beyond
    public sealed class GraphqlDocumentWriter : GraphQL.Http.IDocumentWriter
    {
        private readonly GraphQL.Http.DocumentWriter _innerWriter = new GraphQL.Http.DocumentWriter();
        public string Write(object value)
        {
            return _innerWriter.Write(value);
        }

        public Task WriteAsync<T>(Stream stream, T value)
        {
            var data = JsonConvert.SerializeObject(value);

            var bytes = System.Text.Encoding.UTF8.GetBytes(data);

            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public Task<GraphQL.Http.IByteResult> WriteAsync<T>(T value)
        {
            return _innerWriter.WriteAsync(value);
        }
    }
}