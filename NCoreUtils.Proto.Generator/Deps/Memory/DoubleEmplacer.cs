using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class DoubleEmplacer : IEmplacer<double>
    {
        public const int DefaultMaxPrecision = 8;

        public const string DefaultDecimalSeparator = ".";

        public static DoubleEmplacer Default { get; } = new DoubleEmplacer();

        public int MaxPrecision { get; }

        public string DecimalSeparator { get; }

        public DoubleEmplacer(int maxPrecision = DefaultMaxPrecision, string decimalSeparator = DefaultDecimalSeparator)
        {
            MaxPrecision = maxPrecision;
            DecimalSeparator = decimalSeparator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(double value, Span<char> span)
            => Emplacer.Emplace(value, span, MaxPrecision, DecimalSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(double value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}