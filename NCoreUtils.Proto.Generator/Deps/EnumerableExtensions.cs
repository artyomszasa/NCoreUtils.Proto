// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Extensions.Collections

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NCoreUtils
{
    public static class EnumerableExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T item)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(predicate));
                case IList<T> list:
                    {
                        if (0 == list.Count)
                        {
                            item = default!;
                            return false;
                        }
                        var found = false;
                        item = default!;
                        for (var i = 0; !found && i < list.Count; ++i)
                        {
                            var current = list[i];
                            if (predicate(current))
                            {
                                found = true;
                                item = current;
                            }
                        }
                        return found;
                    }
                case IReadOnlyList<T> list:
                    {
                        if (0 == list.Count)
                        {
                            item = default!;
                            return false;
                        }
                        var found = false;
                        item = default!;
                        for (var i = 0; !found && i < list.Count; ++i)
                        {
                            var current = list[i];
                            if (predicate(current))
                            {
                                found = true;
                                item = current;
                            }
                        }
                        return found;
                    }
                default:
                    using (var enumerator = source.GetEnumerator())
                    {
                        var found = false;
                        item = default!;
                        while (!found && enumerator.MoveNext())
                        {
                            var current = enumerator.Current;
                            if (predicate(current))
                            {
                                found = true;
                                item = current;
                            }
                        }
                        return found;
                    }
            }
        }
    }
}