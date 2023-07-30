// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Text

using System;
using System.Globalization;
#if !NETSTANDARD2_1
using System.Text;
#endif


namespace NCoreUtils.Text
{
    public class SnakeCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Snake case may add an additional underscores after each two characters (sequential uppercased characters
        /// treated as acronyms).
        /// </summary>
        /// <param name="sourceCharCount">Source character count.</param>
        /// <returns>
        /// Maximum number of characters needed to store input of the specified length as snake case string.
        /// </returns>
        public int GetMaxCharCount(int sourceCharCount)
            => sourceCharCount + (sourceCharCount % 2 == 0
                ? sourceCharCount / 2
                : sourceCharCount / 2 + 1);

        /// <summary>
        /// Attempts to convert name specified in <paramref name="source" /> to snake case naming convention. On success
        /// <paramref name="written" /> contains count of the characters written. If the buffer is insuffiicient returns
        /// <c>false</c> and the buffer may be partially written.
        /// </summary>
        /// <param name="source">Name to convert.</param>
        /// <param name="destination">Buffer to store converted name.</param>
        /// <param name="written">Bytes written.</param>
        /// <returns>
        /// <c>true</c> if the buffer is sufficient to store the converted name and conversion has been successfull,
        /// <c>false</c> otherwise.
        /// </returns>
        public bool TryApply(ReadOnlySpan<char> source, Span<char> destination, out int written)
        {
            var culture = CultureInfo.InvariantCulture;
            var builder = new SpanBuilder(destination);
            var isDelimiter = true;
            var acronym = false;
            foreach (var rune in source.EnumerateRunes())
            {
                if (Rune.IsLetterOrDigit(rune))
                {
                    // rune is writable character!
                    if (Rune.IsUpper(rune))
                    {
                        // rune is uppercase --> if it is neither part of acronym nor starts a word --> add underscore.
                        if (!(isDelimiter || acronym))
                        {
                            if (!builder.TryAppend('_'))
                            {
                                written = default;
                                return false;
                            }
                        }
                        if (!builder.TryAppend(Rune.ToLower(rune, culture)))
                        {
                            written = default;
                            return false;
                        }
                        acronym = true;
                    }
                    else
                    {
                        // just copy it...
                        if (!builder.TryAppend(rune))
                        {
                            written = default;
                            return false;
                        }
                        acronym = false;
                    }
                    isDelimiter = false;
                }
                else
                {
                    if (!isDelimiter)
                    {
                        // close word
                        if (!builder.TryAppend('_'))
                        {
                            written = default;
                            return false;
                        }
                        isDelimiter = true;
                    }
                }
            }
            written = builder.Length;
            // handle trailing undescore...
            if (written > 0 && destination[written - 1] == '_')
            {
                --written;
            }
            return true;
        }
    }
}