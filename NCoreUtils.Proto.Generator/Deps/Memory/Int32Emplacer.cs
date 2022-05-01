using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class Int32Emplacer : IEmplacer<int>
    {
        public static Int32Emplacer Instance { get; } = new Int32Emplacer();

        private Int32Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(int value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(int value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}