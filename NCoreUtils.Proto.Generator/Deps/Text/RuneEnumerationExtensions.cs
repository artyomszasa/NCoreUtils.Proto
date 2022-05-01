using System;

namespace NCoreUtils.Text
{
    internal static class RuneEnumerationExtensions
    {
        public static SpanRuneEnumerator EnumerateRunes(this ReadOnlySpan<char> source)
            => new(source);
    }
}