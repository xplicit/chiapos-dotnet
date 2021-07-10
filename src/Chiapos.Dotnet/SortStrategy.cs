namespace Chiapos.Dotnet
{
    public enum SortStrategy : byte
    {
        Uniform,
        Quicksort,

        // the quicksort_last strategy is important because uniform sort performs
        // really poorly on data that isn't actually uniformly distributed. The last
        // buckets are often not uniformly distributed.
        QuicksortLast,
    }
}