namespace Chiapos.Dotnet
{
    public class SR
    {
        public const string Argument_ArrayTooLarge = "The input array length must not exceed Int32.MaxValue / {0}. Otherwise BitArray.Length would exceed Int32.MaxValue.";
        public const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
        public const string Arg_ArrayLengthsDiffer = "Array lengths must be the same.";
        public const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
        public const string Argument_InvalidOffLen = "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";
        public const string Arg_BitArrayTypeUnsupported = "Only supported array types for CopyTo on BitArrays are Boolean[], Int32[] and Byte[].";
        public const string ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";
        public const string InvalidOperation_EnumFailedVersion = "Collection was modified after the enumerator was instantiated.";
        public const string InvalidOperation_EnumNotStarted = "Enumeration has not started. Call MoveNext.";
        public const string InvalidOperation_EnumEnded = "Enumeration already finished.";
        public static string Format(string format, params object[] args) => string.Format(format, args);
        
    }
}