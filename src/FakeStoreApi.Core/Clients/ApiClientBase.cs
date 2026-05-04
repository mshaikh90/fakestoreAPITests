using FakeStoreApi.Core.Configuration;
using FakeStoreApi.Core.Http;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace FakeStoreApi.Core.Clients;

public abstract class ApiClientBase
{
    private readonly RestClient _client;

    protected ApiClientBase()
        : this(ApiClientOptions.FromConfiguration())
    {
    }

    protected ApiClientBase(ApiClientOptions options, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _client = SharedRestClient.GetOrCreate(options, loggerFactory);
    }

    protected ApiClientBase(RestClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    protected Task<ApiResponse<T>> GetAsync<T>(string resource, CancellationToken cancellationToken = default) =>
        ExecuteAsync<T>(new RestRequest(resource, Method.Get), cancellationToken);

    protected Task<ApiResponse<T>> DeleteAsync<T>(string resource, CancellationToken cancellationToken = default) =>
        ExecuteAsync<T>(new RestRequest(resource, Method.Delete), cancellationToken);

    protected Task<ApiResponse<T>> PostJsonAsync<T>(
        string resource,
        object body,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(resource, Method.Post, body, cancellationToken);

    protected Task<ApiResponse<T>> PutJsonAsync<T>(
        string resource,
        object body,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<T>(resource, Method.Put, body, cancellationToken);

    private Task<ApiResponse<T>> SendJsonAsync<T>(
        string resource,
        Method method,
        object body,
        CancellationToken cancellationToken)
    {
        var request = new RestRequest(resource, method)
            .AddJsonBody(body);

        return ExecuteAsync<T>(request, cancellationToken);
    }

    private async Task<ApiResponse<T>> ExecuteAsync<T>(
        RestRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.ExecuteAsync<T>(request, cancellationToken);

        return new ApiResponse<T>
        {
            Method = request.Method.ToString().ToUpperInvariant(),
            Resource = request.Resource,
            StatusCode = response.StatusCode,
            IsSuccessful = response.IsSuccessful,
            ContentType = response.ContentType,
            RawContent = response.Content,
            ResponseUri = response.ResponseUri,
            Data = response.Data,
            ErrorMessage = response.ErrorMessage,
            ErrorException = response.ErrorException,
            Headers = ToHeaderDictionary(response.Headers)
        };
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ToHeaderDictionary(
        IEnumerable<HeaderParameter>? headers)
    {
        if (headers is null)
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        return headers
            .Where(header => !string.IsNullOrWhiteSpace(header.Name))
            .GroupBy(header => header.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(header => header.Value?.ToString() ?? string.Empty)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
