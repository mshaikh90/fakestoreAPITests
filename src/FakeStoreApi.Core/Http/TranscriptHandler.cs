using System.Diagnostics;
using FakeStoreApi.Core.Diagnostics;

namespace FakeStoreApi.Core.Http;

internal sealed class TranscriptHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadBodyAsync(request.Content, cancellationToken);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await ReadBodyAsync(response.Content, cancellationToken);

            HttpTranscript.Record(new HttpTranscriptEntry
            {
                Method = request.Method.Method,
                Uri = request.RequestUri,
                RequestContentType = request.Content?.Headers.ContentType?.ToString(),
                RequestBody = HttpRedactor.Redact(requestBody),
                StatusCode = response.StatusCode,
                ReasonPhrase = response.ReasonPhrase,
                ResponseContentType = response.Content.Headers.ContentType?.ToString(),
                ResponseBody = HttpRedactor.Redact(responseBody),
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            });

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            HttpTranscript.Record(new HttpTranscriptEntry
            {
                Method = request.Method.Method,
                Uri = request.RequestUri,
                RequestContentType = request.Content?.Headers.ContentType?.ToString(),
                RequestBody = HttpRedactor.Redact(requestBody),
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                ExceptionMessage = exception.Message
            });

            throw;
        }
    }

    private static async Task<string?> ReadBodyAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null || !IsTextContent(content))
            return null;

        var body = await content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static bool IsTextContent(HttpContent content)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        return mediaType is null
               || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
               || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
               || mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase);
    }
}
