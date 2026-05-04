using System.Diagnostics;
using FakeStoreApi.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeStoreApi.Core.Http;

internal sealed class LoggingHandler(ILogger logger, ApiClientOptions options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var redactedUri = HttpRedactor.Redact(request.RequestUri?.ToString());

        logger.LogInformation("--> {Method} {Uri}", request.Method, redactedUri);

        if (options.LogRequestBody)
        {
            var requestBody = await ReadBodyAsync(request.Content, cancellationToken);
            if (!string.IsNullOrWhiteSpace(requestBody))
                logger.LogInformation("    Body: {Body}", FormatBody(requestBody));
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            logger.LogInformation(
                "<-- {Method} {Uri} {StatusCode} {ReasonPhrase} ({ElapsedMilliseconds} ms)",
                request.Method,
                redactedUri,
                (int)response.StatusCode,
                response.ReasonPhrase,
                stopwatch.ElapsedMilliseconds);

            if (options.LogResponseBody)
            {
                var responseBody = await ReadBodyAsync(response.Content, cancellationToken);
                if (!string.IsNullOrWhiteSpace(responseBody))
                    logger.LogInformation("    Body: {Body}", FormatBody(responseBody));
            }

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "<-- {Method} {Uri} failed ({ElapsedMilliseconds} ms)",
                request.Method,
                redactedUri,
                stopwatch.ElapsedMilliseconds);

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

    private string FormatBody(string body)
    {
        var redacted = HttpRedactor.Redact(body) ?? string.Empty;
        return redacted.Length <= options.MaxLoggedBodyLength
            ? redacted
            : string.Concat(redacted.AsSpan(0, options.MaxLoggedBodyLength), "... [truncated]");
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
