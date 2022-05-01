using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Memory
{
    public class EmplaceableEmplacer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IEmplacer<T>
        where T : IEmplaceable<T>
    {
        public int Emplace(T value, Span<char> span)
            => value.Emplace(span);

        public bool TryEmplace(T value, Span<char> span, out int used)
            => value.TryEmplace(span, out used);
    }
}