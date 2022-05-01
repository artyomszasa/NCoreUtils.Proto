using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class Int8Emplacer : IEmplacer<sbyte>
    {
        public static Int8Emplacer Instance { get; } = new Int8Emplacer();

        Int8Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(sbyte value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(sbyte value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}