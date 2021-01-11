using GraphQL;
using GraphQL.Execution;

namespace OttoTheGeek.Internal
{
    // this class overrides the default behavior to run queries in parallel
    public sealed class OttoDocumentExecuter : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            if(context.Operation.OperationType == GraphQL.Language.AST.OperationType.Query)
            {
                return new SerialExecutionStrategy();
            }

            return base.SelectExecutionStrategy(context);
        }
    }
}