using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class Int64Emplacer : IEmplacer<long>
    {
        public static Int64Emplacer Instance { get; } = new Int64Emplacer();

        private Int64Emplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(long value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(long value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}