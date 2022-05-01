using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class SingleEmplacer : IEmplacer<float>
    {
        public const int DefaultMaxPrecision = 8;

        public const string DefaultDecimalSeparator = ".";

        public static SingleEmplacer Default { get; } = new SingleEmplacer();

        public int MaxPrecision { get; }

        public string DecimalSeparator { get; }

        public SingleEmplacer(int maxPrecision = DefaultMaxPrecision, string decimalSeparator = DefaultDecimalSeparator)
        {
            MaxPrecision = maxPrecision;
            DecimalSeparator = decimalSeparator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(float value, Span<char> span)
            => Emplacer.Emplace(value, span, MaxPrecision, DecimalSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(float value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, MaxPrecision, DecimalSeparator, out used);
    }
}