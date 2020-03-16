namespace OttoTheGeek.RuntimeSchema
{
    public sealed class FieldArgument
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ObjectType Type { get; set; }
        public string DefaultValue { get; set; }
    }
}