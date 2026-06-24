using System.Text.RegularExpressions;

namespace OnlineStore.Api.Helpers;

public static partial class OutputSanitizer
{
    // Remove leaked internal function-call syntax like "function=tool_name"
    [GeneratedRegex(@"function=\w+", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionLeakRegex();

    // Remove XML/HTML tags that should never appear in chat responses
    [GeneratedRegex(@"<[a-zA-Z][^>]*>|</[a-zA-Z]+>")]
    private static partial Regex XmlLeakRegex();

    // Match JSON that looks like a raw backend object leak:
    // e.g. {"productId":123,"stock":5} — has int/null values, no natural language
    [GeneratedRegex(@"\{[^{}]*""[A-Za-z]+"":\s*(\d+|null|true|false)[^{}]*\}")]
    private static partial Regex JsonObjectLeakRegex();

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var result = input;

        // Remove leaked function-call syntax
        result = FunctionLeakRegex().Replace(result, "");

        // Remove leaked XML/HTML tags
        result = XmlLeakRegex().Replace(result, "");

        // Only strip raw JSON objects that look like backend data leaks
        // (has typed values like integers or booleans, not natural language)
        result = JsonObjectLeakRegex().Replace(result, "");

        // Clean up double spaces or stray newlines
        result = result.Replace("  ", " ").Trim();

        return result;
    }
}
