using System.Diagnostics.CodeAnalysis;

namespace AimuBot.Core.Extensions;

public static class StringExtension
{
    static readonly System.Globalization.CultureInfo default_culture = new("en-US");

    public static string Drop(this string s, int n) => s[n..];
    public static string DropLast(this string s, int n) => s[^n..];

    public static bool StartsWith(this string s, string value, bool ignoreCase) =>
        s.StartsWith(value, ignoreCase, default_culture);

    public static bool Contains(this string s, string value, bool ignoreCase) =>
        s.Contains(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    public static string SubstringAfter(this string s, string value)
    {
        int pos = s.IndexOf(value);
        if (pos == -1)
            return string.Empty;
        return s[(pos + value.Length)..];
    }

    public static string SubstringAfterLast(this string s, string value)
    {
        int pos = s.LastIndexOf(value);
        if (pos == -1)
            return string.Empty;
        return s[(pos + value.Length)..];
    }

    public static string SubstringBefore(this string s, string value)
    {
        int pos = s.IndexOf(value);
        if (pos == -1)
            return string.Empty;
        return s[..pos];
    }

    public static string SubstringBeforeLast(this string s, string value)
    {
        int pos = s.LastIndexOf(value);
        if (pos == -1)
            return string.Empty;
        return s[..pos];
    }

    public static bool IsEmpty(this string s) => s == "";

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s) => string.IsNullOrEmpty(s);

    public static string GetSandwichedText(this string s, string left, string right)
    {
        if (left.IsEmpty())
        {
            if (s.Contains(right))
                return s.SubstringBefore(right);
        }
        else if (right.IsEmpty())
        {
            if (s.Contains(left))
                return s.SubstringAfter(left);
        }
        else
        {
            if (s.Contains(left) && s.Contains(right))
                return s.SubstringAfter(left).SubstringBefore(right);
        }
        return string.Empty;
    }

    public static string CombinePath(this string s, string path) => Path.Combine(s, path);

    public static string UnEscapeMiraiCode(this string s) => s.Replace("\\[", "[").Replace("\\]", "]").Replace("\\n", "\n").Replace("\\r", "\r")
            .Replace("\\:", ":").Replace("\\,", ",")
            .Replace("\\\\", "\\");

}
