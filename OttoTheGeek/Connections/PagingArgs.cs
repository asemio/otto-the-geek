namespace OttoTheGeek.Connections
{
    public class PagingArgs<T>
    {
        public int Offset { get; set; }
        public int Count { get; set; }
        public OrderValue<T> OrderBy { get; set; }
    }
}