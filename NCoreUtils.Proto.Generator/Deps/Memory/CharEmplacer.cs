using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class CharEmplacer : IEmplacer<char>
    {
        public static CharEmplacer Instance { get; } = new CharEmplacer();

        private CharEmplacer() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(char value, Span<char> span)
            => Emplacer.Emplace(value, span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(char value, Span<char> span, out int used)
        {
            if (span.Length < 1)
            {
                used = default;
                return false;
            }
            span[0] = value;
            used = 1;
            return true;
        }
    }
}