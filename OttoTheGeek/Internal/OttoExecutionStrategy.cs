using System.Collections.Generic;
using GraphQL;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace OttoTheGeek.Internal
{
    // this class overrides the default behavior to run queries in parallel
    public sealed class OttoDocumentExecuter : DocumentExecuter
    {
        public OttoDocumentExecuter(IDocumentBuilder documentBuilder,
            IDocumentValidator documentValidator,
            IExecutionStrategySelector executionStrategySelector,
            IEnumerable<IConfigureExecution> configurations
            )
            : base(documentBuilder, documentValidator, executionStrategySelector, configurations)
        {
            
        }
        
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            if(context.Operation.Operation == OperationType.Query)
            {
                return new SerialExecutionStrategy();
            }

            return base.SelectExecutionStrategy(context);
        }
    }
}
