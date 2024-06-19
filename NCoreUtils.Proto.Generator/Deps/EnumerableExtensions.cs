// see: https://github.com/dotnet/roslyn/issues/43903
// NCoreUtils.Extensions.Collections

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct Choice<T>(T value)
    {
        private readonly bool hasValue = true;

        private readonly T value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue([MaybeNullWhen(false)] out T item)
        {
            if (hasValue)
            {
                item = value;
                return true;
            }
            item = default;
            return false;
        }
    }

    public static class Choice
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Choice<T> Choose<T>(this T value) => new(value);
    }

    public static class EnumerableExtensions
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryChooseFirst<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, Choice<TResult>> chooser, [MaybeNullWhen(false)] out TResult result)
        {
            if (source.IsDefaultOrEmpty)
            {
                result = default;
                return false;
            }
            foreach (var it in source)
            {
                if (chooser(it).TryGetValue(out var res))
                {
                    result = res;
                    return true;
                }
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryChooseFirst<TSource, TArg, TResult>(this ImmutableArray<TSource> source, TArg arg, Func<TSource, TArg, Choice<TResult>> chooser, [MaybeNullWhen(false)] out TResult result)
        {
            if (source.IsDefaultOrEmpty)
            {
                result = default;
                return false;
            }
            foreach (var it in source)
            {
                if (chooser(it, arg).TryGetValue(out var res))
                {
                    result = res;
                    return true;
                }
            }
            result = default;
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(predicate));
                case ImmutableArray<T> array:
                    {
                        var enumerator = array.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var it = enumerator.Current;
                            if (predicate(it))
                            {
                                item = it;
                                return true;
                            }
                        }
                        item = default;
                        return false;
                    }
                case IList<T> list:
                    {
                        if (0 == list.Count)
                        {
                            item = default;
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
                            item = default;
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
                        item = default;
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