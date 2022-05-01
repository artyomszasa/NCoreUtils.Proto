using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class Int16Emplacer : IEmplacer<short>
    {
        public static Int16Emplacer Instance { get; } = new Int16Emplacer();

        private Int16Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(short value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(short value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}