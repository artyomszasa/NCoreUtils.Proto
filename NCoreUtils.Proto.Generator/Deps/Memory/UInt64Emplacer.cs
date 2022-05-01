using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class UInt64Emplacer : IEmplacer<ulong>
    {
        public static UInt64Emplacer Instance { get; } = new UInt64Emplacer();

        private UInt64Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(ulong value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(ulong value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}