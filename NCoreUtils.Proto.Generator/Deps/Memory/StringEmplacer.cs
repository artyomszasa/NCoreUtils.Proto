using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class StringEmplacer : IEmplacer<string>
    {
        public static StringEmplacer Instance { get; } = new StringEmplacer();

        private StringEmplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(string value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(string value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value, span, out used);
    }
}