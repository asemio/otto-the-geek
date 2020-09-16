namespace OttoTheGeek
{
    public abstract class ScalarTypeConverter<TScalar>
    {
        public abstract string Convert(TScalar value);
        public abstract TScalar Parse(string value);
    }
}