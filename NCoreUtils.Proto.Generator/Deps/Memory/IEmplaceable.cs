using System;

namespace NCoreUtils.Memory
{
    public interface IEmplaceable<T>
    {
#if NETFRAMEWORK || NETSTANDARD2_0
        int Emplace(Span<char> span);
#else
        int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var used))
            {
                return used;
            }
            throw new InsufficientBufferSizeException(span);
        }
#endif

        bool TryEmplace(Span<char> span, out int used);
    }
}