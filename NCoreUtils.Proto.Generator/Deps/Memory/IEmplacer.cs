using System;

namespace NCoreUtils.Memory
{
    public interface IEmplacer<T>
    {
        int Emplace(T value, Span<char> span);

        bool TryEmplace(T value, Span<char> span, out int used);
    }
}