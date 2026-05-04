using System.Text.RegularExpressions;

namespace FakeStoreApi.Core.Http;

internal static class HttpRedactor
{
    private static readonly Regex QueryStringSecrets = new(
        "(?i)(authorization|token|api[_-]?key|password|secret)=([^&\\s]+)",
        RegexOptions.Compiled);

    private static readonly Regex JsonSecrets = new(
        "(?i)(\"(?:authorization|token|apiKey|password|secret)\"\\s*:\\s*\")[^\"]*(\")",
        RegexOptions.Compiled);

    public static string? Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var redacted = QueryStringSecrets.Replace(value, "$1=***");
        return JsonSecrets.Replace(redacted, "$1***$2");
    }
}
