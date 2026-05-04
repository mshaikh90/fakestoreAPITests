using System.Net;

namespace FakeStoreApi.Core.Diagnostics;

public sealed record HttpTranscriptEntry
{
    public required string Method { get; init; }

    public required Uri? Uri { get; init; }

    public string? RequestContentType { get; init; }

    public string? RequestBody { get; init; }

    public HttpStatusCode? StatusCode { get; init; }

    public string? ReasonPhrase { get; init; }

    public string? ResponseContentType { get; init; }

    public string? ResponseBody { get; init; }

    public long ElapsedMilliseconds { get; init; }

    public string? ExceptionMessage { get; init; }
}
