// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Extensions.Memory

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NCoreUtils.Memory;

namespace NCoreUtils
{
    public static class Emplacer
    {
        private static readonly Dictionary<Type, object> _emplacers = new()
        {
            { typeof(sbyte), Int8Emplacer.Instance },
            { typeof(short), Int16Emplacer.Instance },
            { typeof(int), Int32Emplacer.Instance },
            { typeof(long), Int64Emplacer.Instance },
            { typeof(byte), UInt8Emplacer.Instance },
            { typeof(ushort), UInt16Emplacer.Instance },
            { typeof(uint), UInt32Emplacer.Instance },
            { typeof(ulong), UInt64Emplacer.Instance },
            { typeof(float), SingleEmplacer.Default },
            { typeof(double), DoubleEmplacer.Default },
            { typeof(char), CharEmplacer.Instance },
            { typeof(string), StringEmplacer.Instance }
        };

        private static ulong DivRem(ulong a, ulong b, out ulong reminder)
        {
            reminder = a % b;
            return a / b;
        }

        private static char I(int value) => (char)('0' + value);

        #region char

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryEmplaceInternal(char value, Span<char> span, out int total)
        {
            total = 1;
            if (span.Length < 1)
            {
                return false;
            }
            span[0] = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(char value, Span<char> span)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(char value, Span<char> span, out int used)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        #endregion

        #region sbyte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(sbyte value, Span<char> span)
            => Emplace((int)value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(sbyte value, Span<char> span, out int used)
            => TryEmplace((int)value, span, out used);

        #endregion

        #region short

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(short value, Span<char> span)
            => Emplace((int)value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(short value, Span<char> span, out int used)
            => TryEmplace((int)value, span, out used);

        #endregion

        #region int

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEmplaceInternal(int value, Span<char> span, out int total)
        {
            if (value == int.MinValue)
            {
                return TryEmplaceInternal("-2147483648", span, out total);
            }
            if (0 == value)
            {
                return TryEmplaceInternal('0', span, out total);
            }
            var isSigned = value < 0 ? 1 : 0;
            value = Math.Abs(value);
            total = (int)Math.Floor(Math.Log10(value)) + 1 + isSigned;
            if (span.Length < total)
            {
                return false;
            }
            if (0 != isSigned)
            {
                span[0] = '-';
            }
            for (var offset = isSigned; value > 0; ++offset)
            {
                value = Math.DivRem(value, 10, out var part);
                span[offset] = I(part);
            }
            if (0 != isSigned)
            {
#if NETFRAMEWORK
                span.Slice(1, total - 1).Reverse();
#else
                span[1..total].Reverse();
#endif
            }
            else
            {
#if NETFRAMEWORK
                span.Slice(0, total).Reverse();
#else
                span[..total].Reverse();
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(int value, Span<char> span)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(int value, Span<char> span, out int used)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        #endregion

        #region long

        private static bool TryEmplaceInternal(long value, Span<char> span, out int total)
        {
            if (value == long.MinValue)
            {
                return TryEmplaceInternal("-9223372036854775808", span, out total);
            }
            if (0L == value)
            {
                return TryEmplaceInternal('0', span, out total);
            }
            var isSigned = value < 0L ? 1 : 0;
            value = Math.Abs(value);
            total = (int)Math.Floor(Math.Log10(value)) + 1 + isSigned;
            if (span.Length < total)
            {
                return false;
            }
            if (0 != isSigned)
            {
                span[0] = '-';
            }
            for (var offset = isSigned; value > 0L; ++offset)
            {
                value = Math.DivRem(value, 10, out var part);
                unchecked
                {
                    span[offset] = I((int)part);
                }
            }
            if (0 != isSigned)
            {
#if NETFRAMEWORK
                span.Slice(1, total - 1).Reverse();
#else
                span[1..total].Reverse();
#endif
            }
            else
            {
#if NETFRAMEWORK
                span.Slice(0, total).Reverse();
#else
                span[..total].Reverse();
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(long value, Span<char> span)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(long value, Span<char> span, out int used)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        #endregion

        #region float

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEmplaceInternal(float value, Span<char> span, int maxPrecision, string decimalSeparator, out int total)
        {
            if (0.0f == value)
            {
                return TryEmplaceInternal('0', span, out total);
            }
            Span<char> fbuffer = stackalloc char[maxPrecision];
            var uvalue = Math.Abs(value);
            // intgeral part
            var ivalue = (int)value;
            var isNegative = ivalue < 0 ? 1 : 0;
            var uivalue = Math.Abs(ivalue);
            // floating part
            var fvalue = uvalue - uivalue;
            // intgeral part length...
            var ilength = (int)Math.Floor(Math.Log10(uivalue)) + 1 + isNegative;
            // stringify floating part locally to get value...
            var flength = 0;
            var flast = maxPrecision - 1;
            while (flength < maxPrecision)
            {
                var v = fvalue * Math.Pow(10.0, flength + 1);
                if (0.0 == v % 10.0)
                {
                    break;
                }
                if (flength == flast)
                {
                    fbuffer[flength] = I((int)Math.Round(v) % 10);
                }
                else
                {
                    fbuffer[flength] = I((int)v % 10);
                }
                ++flength;
            }
            total = ilength + (flength == 0 ? 0 : flength + decimalSeparator.Length);
            if (span.Length < total)
            {
                return false;
            }
            Emplace(ivalue, span);
            if (flength > 0)
            {
#if NETFRAMEWORK
                decimalSeparator.AsSpan().CopyTo(span.Slice(ilength));
                fbuffer.Slice(0, flength).CopyTo(span.Slice(ilength + decimalSeparator.Length));
#else
                decimalSeparator.AsSpan().CopyTo(span[ilength..]);
                fbuffer[..flength].CopyTo(span[(ilength + decimalSeparator.Length)..]);
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(float value, Span<char> span, int maxPrecision, string decimalSeparator = ".")
        {
            if (TryEmplaceInternal(value, span, maxPrecision, decimalSeparator, out var length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(float value, Span<char> span, int maxPrecision, string decimalSeparator, out int used)
        {
            if (TryEmplaceInternal(value, span, maxPrecision, decimalSeparator, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(float value, Span<char> span, int maxPrecision, out int used)
            => TryEmplace(value, span, maxPrecision, SingleEmplacer.DefaultDecimalSeparator, out used);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(float value, Span<char> span, out int used)
            => TryEmplace(value, span, SingleEmplacer.DefaultMaxPrecision, SingleEmplacer.DefaultDecimalSeparator, out used);

        #endregion

        #region double

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEmplaceInternal(double value, Span<char> span, int maxPrecision, string decimalSeparator, out int total)
        {
            if (0.0 == value)
            {
                return TryEmplaceInternal('0', span, out total);
            }
            Span<char> fbuffer = stackalloc char[maxPrecision];
            var uvalue = Math.Abs(value);
            // intgeral part
            var ivalue = (long)value;
            var isNegative = ivalue < 0L ? 1 : 0;
            var uivalue = Math.Abs(ivalue);
            // floating part
            var fvalue = uvalue - uivalue;
            // intgeral part length...
            var ilength = (int)Math.Floor(Math.Log10(uivalue)) + 1 + isNegative;
            // stringify floating part locally to get value...
            var flength = 0;
            var flast = maxPrecision - 1;
            while (flength < maxPrecision)
            {
                var v = fvalue * Math.Pow(10.0, flength + 1);
                if (0.0 == v % 10.0)
                {
                    break;
                }
                if (flength == flast)
                {
                    fbuffer[flength] = I((int)Math.Round(v) % 10);
                }
                else
                {
                    fbuffer[flength] = I((int)v % 10);
                }
                ++flength;
            }
            total = ilength + (flength == 0 ? 0 : flength + decimalSeparator.Length);
            if (span.Length < total)
            {
                return false;
            }
            Emplace(ivalue, span);
            if (flength > 0)
            {
#if NETFRAMEWORK
                decimalSeparator.AsSpan().CopyTo(span.Slice(ilength));
                fbuffer.Slice(0, flength).CopyTo(span.Slice(ilength + decimalSeparator.Length));
#else
                decimalSeparator.AsSpan().CopyTo(span[ilength..]);
                fbuffer[..flength].CopyTo(span[(ilength + decimalSeparator.Length)..]);
#endif
            }
            return true;

            static char I(int value) => (char)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(double value, Span<char> span, int maxPrecision, string decimalSeparator = ".")
        {
            if (TryEmplaceInternal(value, span, maxPrecision, decimalSeparator, out var length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(double value, Span<char> span, int maxPrecision, string decimalSeparator, out int used)
        {
            if (TryEmplaceInternal(value, span, maxPrecision, decimalSeparator, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(double value, Span<char> span, int maxPrecision, out int used)
            => TryEmplace(value, span, maxPrecision, DoubleEmplacer.DefaultDecimalSeparator, out used);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(double value, Span<char> span, out int used)
            => TryEmplace(value, span, DoubleEmplacer.DefaultMaxPrecision, DoubleEmplacer.DefaultDecimalSeparator, out used);

        #endregion

        #region string

        public static bool TryEmplaceInternal(string? value, Span<char> span, out int total)
        {
            if (value is null)
            {
                total = 0;
                return true;
            }
            total = value.Length;
            if (value.Length > span.Length)
            {
                return false;
            }
            value.AsSpan().CopyTo(span);
            return true;
        }

        public static int Emplace(string? value, Span<char> span)
        {
            if (TryEmplaceInternal(value, span, out var length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        public static bool TryEmplace(string? value, Span<char> span, out int used)
        {
            if (TryEmplaceInternal(value, span, out var length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        #endregion

        #region byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(byte value, Span<char> span)
            => Emplace((int)value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(byte value, Span<char> span, out int used)
            => TryEmplace((int)value, span, out used);

        #endregion

        #region ushort

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(ushort value, Span<char> span)
            => Emplace((int)value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(ushort value, Span<char> span, out int used)
            => TryEmplace((int)value, span, out used);

        #endregion

        #region uint

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(uint value, Span<char> span)
            => Emplace((long)value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(uint value, Span<char> span, out int used)
            => TryEmplace((long)value, span, out used);


        #endregion

        #region ulong

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryEmplaceInternal(ulong value, Span<char> span, out int total)
        {
            if (0L == value)
            {
                return TryEmplaceInternal('0', span, out total);
            }
            total = (int)Math.Floor(Math.Log10(value)) + 1;
            if (span.Length < total)
            {
                return false;
            }
            for (var offset = 0; value != 0; ++offset)
            {
                value = DivRem(value, 10L, out var part);
                unchecked
                {
                    span[offset] = I((int)part);
                }
            }
#if NETFRAMEWORK
            span.Slice(0, total).Reverse();
#else
            span[..total].Reverse();
#endif
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace(ulong value, Span<char> span)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                return length;
            }
            throw new InsufficientBufferSizeException(span, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace(ulong value, Span<char> span, out int used)
        {
            if (TryEmplaceInternal(value, span, out int length))
            {
                used = length;
                return true;
            }
            used = default;
            return false;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emplace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T value, Span<char> span)
            => GetDefault<T>().Emplace(value, span);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEmplace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T value, Span<char> span, out int used)
            => GetDefault<T>().TryEmplace(value, span, out used);


        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EmplaceableEmplacer<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
            Justification = "Type is bound through attributes.")]
        public static IEmplacer<T> GetDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
        {
            if (_emplacers.TryGetValue(typeof(T), out var boxed))
            {
                return (IEmplacer<T>)boxed;
            }
            if (typeof(IEmplaceable<T>).IsAssignableFrom(typeof(T)))
            {
                return (IEmplacer<T>)Activator.CreateInstance(typeof(EmplaceableEmplacer<>).MakeGenericType(typeof(T)), true)!;
            }
            return new DefaultEmplacer<T>();
        }
    }
}