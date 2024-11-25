using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Log2ui.Collections;

public static class EnumerableExtensions
{
    public static bool TryGetFirstIndex<T>(this IEnumerable<T> source, Predicate<T> predicate, out int index)
    {
        index = 0;
        foreach (var a in source)
        {
            if (predicate(a))
            {
                return true;
            }

            ++index;
        }

        return false;
    }

    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery", Justification = "Null safety")]
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?>? collection)
    {
        foreach (var value in collection ?? Array.Empty<T>())
        {
            if (value is not null)
            {
                yield return value;
            }
        }
    }
}
