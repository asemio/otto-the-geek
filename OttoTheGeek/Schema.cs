namespace OttoTheGeek
{
    public abstract class Schema<TQuery, TMutation, TSubscription>
    {
        public TQuery Query { get; set; }
        public TMutation Mutation { get; set; }
        public TSubscription Subscription { get; set; }
    }
    public abstract class Schema<TQuery> : Schema<TQuery, object, object> { }
    public abstract class Schema<TQuery, TMutation> : Schema<TQuery, TMutation, object> { }
}