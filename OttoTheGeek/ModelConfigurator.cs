namespace OttoTheGeek
{
    public interface IModelConfigurator<TModel>
    {
    }

    public interface IGraphTypeConfigurator<TModel, TType> : IModelConfigurator<TModel>
        where TType : class
    {
        GraphTypeBuilder<TType> Configure(GraphTypeBuilder<TType> builder);
    }
}