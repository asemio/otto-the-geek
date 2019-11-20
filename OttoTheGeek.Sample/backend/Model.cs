namespace OttoTheGeek.Sample
{
    public sealed class Model : OttoModel<Query>
    {
        protected override SchemaBuilder ConfigureSchema(SchemaBuilder builder)
        {
            return builder.GraphType<Query>(b =>
                b.LooseScalarField(x => x.Child)
                    .ResolvesVia<ChildResolver>()
                    );
        }

    }
}
