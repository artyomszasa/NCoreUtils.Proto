using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class UInt8Emplacer : IEmplacer<byte>
    {
        public static UInt8Emplacer Instance { get; } = new UInt8Emplacer();

        private UInt8Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(byte value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(byte value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}