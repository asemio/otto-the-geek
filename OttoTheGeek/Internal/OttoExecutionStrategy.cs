using GraphQL;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace OttoTheGeek.Internal
{
    // this class overrides the default behavior to run queries in parallel
    public sealed class OttoDocumentExecuter : DocumentExecuter
    {
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
