using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

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
        public object Resolve(IResolveFieldContext context)
        {
            if (context.Source == null)
            {
                context = new ProxyFieldContext(context);
            }

            return NameFieldResolver.Instance.Resolve(context);
        }

        private sealed class ProxyFieldContext : IResolveFieldContext
        {
            private readonly IResolveFieldContext _wrapped;

            public ProxyFieldContext(IResolveFieldContext wrapped)
            {
                _wrapped = wrapped;
                Source = Activator.CreateInstance(typeof(T));
            }

            public IDictionary<string, object> UserContext => _wrapped.UserContext;
            public string FieldName => _wrapped.FieldName;
            public Field FieldAst => _wrapped.FieldAst;
            public FieldType FieldDefinition => _wrapped.FieldDefinition;
            public IGraphType ReturnType => _wrapped.ReturnType;
            public IObjectGraphType ParentType => _wrapped.ParentType;
            public IDictionary<string, object> Arguments => _wrapped.Arguments;
            public object RootValue => _wrapped.RootValue;
            public object Source { get; }

            public ISchema Schema => _wrapped.Schema;
            public Document Document => _wrapped.Document;
            public Operation Operation => _wrapped.Operation;
            public Fragments Fragments => _wrapped.Fragments;
            public Variables Variables => _wrapped.Variables;
            public CancellationToken CancellationToken => _wrapped.CancellationToken;
            public Metrics Metrics => _wrapped.Metrics;
            public ExecutionErrors Errors => _wrapped.Errors;
            public IEnumerable<object> Path => _wrapped.Path;
            public IEnumerable<object> ResponsePath => _wrapped.ResponsePath;
            public IDictionary<string, Field> SubFields => _wrapped.SubFields;
            public IDictionary<string, object> Extensions => _wrapped.Extensions;
            public IServiceProvider RequestServices => _wrapped.RequestServices;
        }
    }
}
