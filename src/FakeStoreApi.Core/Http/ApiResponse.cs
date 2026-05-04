using System.Net;

namespace FakeStoreApi.Core.Http;

public sealed class ApiResponse<T>
{
    public required string Method { get; init; }

    public required string Resource { get; init; }

    public string Operation => $"{Method} {Resource}";

    public required HttpStatusCode StatusCode { get; init; }

    public required bool IsSuccessful { get; init; }

    public string? ContentType { get; init; }

    public string? RawContent { get; init; }

    public Uri? ResponseUri { get; init; }

    public T? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public Exception? ErrorException { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
}
