using System;

namespace NineSolsAPI.Utils;

public static class StringExtensions {
    public static ReadOnlySpan<char> TrimEndMatches(this string str, ReadOnlySpan<char> substr) {
        var span = str.AsSpan();
        var idx = span.LastIndexOf(substr);
        return idx == -1 ? span : span[..idx];
    }

    public static ReadOnlySpan<char> TrimEndMatches(this ReadOnlySpan<char> span, ReadOnlySpan<char> substr) {
        var idx = span.LastIndexOf(substr);
        return idx == -1 ? span : span[..idx];
    }

    public static ReadOnlySpan<char> TrimStartMatches(this string str, ReadOnlySpan<char> substr) {
        var span = str.AsSpan();
        var idx = span.LastIndexOf(substr);
        return idx == -1 ? span : span[(idx + substr.Length) ..];
    }

    public static ReadOnlySpan<char> TrimStartMatches(this ReadOnlySpan<char> span, ReadOnlySpan<char> substr) {
        var idx = span.LastIndexOf(substr);
        return idx == -1 ? span : span[(idx + substr.Length) ..];
    }
}