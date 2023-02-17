using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace OttoTheGeek.Internal
{
    /// <summary>
    /// This class exists as a workaround for the case when the topmost type (say, a Query type) has a computed property
    /// or a default-valued property. The IResolveFieldContext in these cases has its Source property set to null; therefore,
    /// its properties can't be resolved, and resolve as null. This class detects this case, instantiates that topmost type,
    /// and resolves its property.
    /// </summary>
    public sealed class PreloadedFieldResolver<T> : IFieldResolver
    {
        public ValueTask<object> ResolveAsync(IResolveFieldContext context)
        {
            if (context.Source == null)
            {
                context = new ProxyFieldContext(context);
            }

            return NameFieldResolver.Instance.ResolveAsync(context);
        }

        private sealed class ProxyFieldContext : IResolveFieldContext
        {
            private readonly IResolveFieldContext _wrapped;

            public ProxyFieldContext(IResolveFieldContext wrapped)
            {
                _wrapped = wrapped;
                Source = Activator.CreateInstance(typeof(T));
            }

            public GraphQLField FieldAst => _wrapped.FieldAst;

            public FieldType FieldDefinition => _wrapped.FieldDefinition;

            public IObjectGraphType ParentType => _wrapped.ParentType;

            public IResolveFieldContext Parent => _wrapped.Parent;

            public IDictionary<string, ArgumentValue> Arguments => _wrapped.Arguments;

            public IDictionary<string, DirectiveInfo> Directives => _wrapped.Directives;

            public object RootValue => _wrapped.RootValue;

            public object Source { get; }
            public ISchema Schema => _wrapped.Schema;

            public GraphQLDocument Document => _wrapped.Document;

            public GraphQLOperationDefinition Operation => _wrapped.Operation;

            public Variables Variables => _wrapped.Variables;

            public CancellationToken CancellationToken => _wrapped.CancellationToken;

            public Metrics Metrics => _wrapped.Metrics;

            public ExecutionErrors Errors => _wrapped.Errors;

            public IEnumerable<object> Path => _wrapped.Path;

            public IEnumerable<object> ResponsePath => _wrapped.ResponsePath;

            public Dictionary<string, (GraphQLField Field, FieldType FieldType)> SubFields => _wrapped.SubFields;

            public IReadOnlyDictionary<string, object> InputExtensions => _wrapped.InputExtensions;

            public IDictionary<string, object> OutputExtensions => _wrapped.OutputExtensions;

            public IServiceProvider RequestServices => _wrapped.RequestServices;

            public IExecutionArrayPool ArrayPool => _wrapped.ArrayPool;

            public ClaimsPrincipal User => _wrapped.User;

            public IDictionary<string, object> UserContext => _wrapped.UserContext;
        }
    }
}
