using System;

namespace NineSolsAPI.Utils;

public static class StringExtensions {
    public static ReadOnlySpan<char> TrimEndMatches(this string str, ReadOnlySpan<char> substr) {
        var span = str.AsSpan();
        var idx = span.LastIndexOf(substr);
        return idx == -1 ? span : span[..idx];
    }
}