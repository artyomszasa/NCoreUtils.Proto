using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class UInt16Emplacer : IEmplacer<ushort>
    {
        public static UInt16Emplacer Instance { get; } = new UInt16Emplacer();

        private UInt16Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(ushort value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(ushort value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}