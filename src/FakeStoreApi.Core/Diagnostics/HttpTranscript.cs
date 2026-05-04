using System.Text;

namespace FakeStoreApi.Core.Diagnostics;

public static class HttpTranscript
{
    private static readonly AsyncLocal<List<HttpTranscriptEntry>?> CurrentEntries = new();

    public static IReadOnlyList<HttpTranscriptEntry> Entries =>
        CurrentEntries.Value?.ToArray() ?? [];

    public static void Clear() => CurrentEntries.Value = [];

    public static void Record(HttpTranscriptEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        CurrentEntries.Value ??= [];
        CurrentEntries.Value.Add(entry);
    }

    public static string FormatForFailure(string? testName = null)
    {
        var entries = Entries;
        var output = new StringBuilder();

        output.AppendLine();
        output.AppendLine("========== HTTP transcript ==========");

        if (!string.IsNullOrWhiteSpace(testName))
            output.AppendLine($"Test: {testName}");

        if (entries.Count == 0)
        {
            output.AppendLine("No HTTP calls were captured for this test.");
            output.AppendLine("=====================================");
            return output.ToString();
        }

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];

            output.AppendLine();
            output.AppendLine($"{index + 1}. {entry.Method} {entry.Uri}");

            if (entry.StatusCode is not null)
            {
                output.AppendLine(
                    $"   Response: {(int)entry.StatusCode.Value} {entry.ReasonPhrase} ({entry.ElapsedMilliseconds} ms)");
            }
            else
            {
                output.AppendLine($"   Response: request failed before a response was received ({entry.ElapsedMilliseconds} ms)");
            }

            if (!string.IsNullOrWhiteSpace(entry.ExceptionMessage))
                output.AppendLine($"   Exception: {entry.ExceptionMessage}");

            if (!string.IsNullOrWhiteSpace(entry.RequestContentType))
                output.AppendLine($"   Request content type: {entry.RequestContentType}");

            AppendBody(output, "Request body", entry.RequestBody);

            if (!string.IsNullOrWhiteSpace(entry.ResponseContentType))
                output.AppendLine($"   Response content type: {entry.ResponseContentType}");

            AppendBody(output, "Response body", entry.ResponseBody);
        }

        output.AppendLine("=====================================");
        return output.ToString();
    }

    private static void AppendBody(StringBuilder output, string label, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;

        output.AppendLine($"   {label}:");

        foreach (var line in body.Split(Environment.NewLine))
            output.AppendLine($"     {line}");
    }

}
