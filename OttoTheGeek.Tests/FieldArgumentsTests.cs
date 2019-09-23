namespace OttoTheGeek.Tests
{
    public sealed class FieldArgumentsTests
    {
        public sealed class Query
        {
            public Child Child { get; set; }
        }

        public sealed class Child
        {
            public string Name => "Imma child!";
        }

        public sealed class Args
        {

        }

        public sealed class Model : OttoModel<Query>
        {
            protected override SchemaBuilder<Query> ConfigureSchema(SchemaBuilder<Query> builder)
            {
                return builder;
                    //.QueryField(x => x.Child)
                    //.Arguments<TArgs>();

            }

        }
    }
}