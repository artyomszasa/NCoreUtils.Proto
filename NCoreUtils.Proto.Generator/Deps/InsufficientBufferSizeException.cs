// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Extensions.Memory

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace NCoreUtils
{
    [Serializable]
    public class InsufficientBufferSizeException : InvalidOperationException
    {
        private const string KeyHasSizeAvailable = "Has" + nameof(SizeAvailable);

        private const string KeyHasSizeRequired = "Has" + nameof(SizeRequired);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatMessage(int sizeAvailable, int? sizeRequired)
            => sizeRequired.HasValue
                ? $"The operation would require storing {sizeRequired.Value} character(s) but the buffer can only accept {sizeAvailable} character(s)."
                : $"The operation would require storing more than {sizeAvailable} character(s) but the buffer can only accept {sizeAvailable} character(s).";

        public int? SizeAvailable { get; }

        public int? SizeRequired { get; }

        protected InsufficientBufferSizeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            SizeAvailable = info.GetBoolean(KeyHasSizeAvailable)
                ? (int?)info.GetInt32(nameof(SizeAvailable))
                : (int?)default;
            SizeRequired = info.GetBoolean(KeyHasSizeRequired)
                ? (int?)info.GetInt32(nameof(SizeRequired))
                : (int?)default;
        }

        [ExcludeFromCodeCoverage]
        [Obsolete("Consider specifying custom message or at least available buffer size.")]
        public InsufficientBufferSizeException()
            : base("Insufficient buffer size")
        { }

        [ExcludeFromCodeCoverage]
        public InsufficientBufferSizeException(string message)
            : base(message)
        { }

        [ExcludeFromCodeCoverage]
        public InsufficientBufferSizeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InsufficientBufferSizeException(int sizeAvailable, int? sizeRequired = default)
            : base(FormatMessage(sizeAvailable, sizeRequired))
        {
            SizeAvailable = sizeAvailable;
            SizeRequired = sizeRequired;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InsufficientBufferSizeException(in Span<char> span, int? sizeRequired = default)
            : this(span.Length, sizeRequired)
        { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(KeyHasSizeAvailable, SizeAvailable.HasValue);
            if (SizeAvailable.HasValue)
            {
                info.AddValue(nameof(SizeAvailable), SizeAvailable.Value);
            }
            info.AddValue(KeyHasSizeRequired, SizeRequired.HasValue);
            if (SizeRequired.HasValue)
            {
                info.AddValue(nameof(SizeRequired), SizeRequired.Value);
            }
        }
    }
}