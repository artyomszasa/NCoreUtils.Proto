using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class UInt32Emplacer : IEmplacer<uint>
    {
        public static UInt32Emplacer Instance { get; } = new UInt32Emplacer();

        private UInt32Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(uint value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(uint value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}