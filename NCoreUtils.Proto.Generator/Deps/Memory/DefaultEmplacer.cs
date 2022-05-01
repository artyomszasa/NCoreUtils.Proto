using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Memory
{
    public sealed class DefaultEmplacer<T> : IEmplacer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Emplace(T value, Span<char> span)
            => Emplacer.Emplace(value?.ToString(), span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEmplace(T value, Span<char> span, out int used)
            => Emplacer.TryEmplace(value?.ToString(), span, out used);
    }
}