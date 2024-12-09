using System;

namespace Log2ui.Extensions;

public static class StringExtensions
{
    public static bool IsGlob(this string value, string globPattern, bool caseSensitive = false)
        => value.AsSpan().IsGlobSpan(globPattern, caseSensitive);

    public static bool IsGlobSpan(this ReadOnlySpan<char> value, ReadOnlySpan<char> globPattern, bool caseSensitive = false)
    {
        var pos = 0;
        while (globPattern.Length != pos)
        {
            switch (globPattern[pos])
            {
                case '?':
                    break;

                case '*':
                    for (var i = value.Length; i >= pos; i--)
                    {
                        if (value.Slice(i).IsGlobSpan(globPattern.Slice(pos + 1), caseSensitive))
                        {
                            return true;
                        }
                    }

                    return false;

                default:
                    if (value.Length == pos)
                    {
                        return false;
                    }

                    if (globPattern[pos] == value[pos])
                    {
                        break;
                    }

                    if (!caseSensitive && char.ToLower(globPattern[pos]) == char.ToLower(value[pos]))
                    {
                        break;
                    }

                    return false;
            }

            pos++;
        }

        return value.Length == pos;
    }
}
