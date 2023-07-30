// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Text

using System;
using System.Globalization;
#if !NETSTANDARD2_1
using System.Text;
#endif


namespace NCoreUtils.Text
{
    public class PascalCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Pascal case names adds no additional characters thus maximum result character count is less than or equals
        /// to the source character count.
        /// </summary>
        /// <param name="sourceCharCount">Source character count.</param>
        /// <returns>
        /// Maximum number of characters needed to store input of the specified length as pascal case string.
        /// </returns>
        public int GetMaxCharCount(int sourceCharCount)
            => sourceCharCount;

        /// <summary>
        /// Attempts to convert name specified in <paramref name="source" /> to pascal case naming convention. On
        /// success <paramref name="written" /> contains count of the characters written. If the buffer is insuffiicient
        /// returns <c>false</c> and the buffer may be partially written.
        /// </summary>
        /// <param name="source">Name to convert.</param>
        /// <param name="destination">Buffer to store converted name.</param>
        /// <param name="written">Bytes written.</param>
        /// <returns>
        /// <c>true</c> if the buffer is sufficient to store the converted name and conversion has benn successfull,
        /// <c>false</c> otherwise.
        /// </returns>
        public bool TryApply(ReadOnlySpan<char> source, Span<char> destination, out int written)
        {
            var culture = CultureInfo.InvariantCulture;
            var builder = new SpanBuilder(destination);
            var isDelimiter = true;
            foreach (var rune in source.EnumerateRunes())
            {
                if (Rune.IsLetterOrDigit(rune))
                {
                    // rune is writable character!
                    if (isDelimiter)
                    {
                        // this rune starts a word --> words start with uppercase character unconditionally
                        if (!builder.TryAppend(Rune.ToUpper(rune, culture)))
                        {
                            written = default;
                            return false;
                        }
                        isDelimiter = false;
                    }
                    else
                    {
                        if (!builder.TryAppend(rune))
                        {
                            written = default;
                            return false;
                        }
                    }
                }
                else
                {
                    isDelimiter = true;
                }
            }
            written = builder.Length;
            return true;
        }
    }
}