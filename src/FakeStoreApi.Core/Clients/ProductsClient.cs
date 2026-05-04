using FakeStoreApi.Core.Configuration;
using FakeStoreApi.Core.Http;
using FakeStoreApi.Core.Models;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace FakeStoreApi.Core.Clients;

public sealed class ProductsClient : ApiClientBase
{
    private const string ProductsResource = "/products";

    public ProductsClient()
    {
    }

    public ProductsClient(ApiClientOptions options, ILoggerFactory? loggerFactory = null)
        : base(options, loggerFactory)
    {
    }

    public ProductsClient(RestClient client)
        : base(client)
    {
    }

    public Task<ApiResponse<List<Product>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<Product>>(ProductsResource, cancellationToken);

    public Task<ApiResponse<Product>> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        GetAsync<Product>(ById(productId), cancellationToken);

    public Task<ApiResponse<Product>> CreateAsync(
        ProductRequest body,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<Product>(ProductsResource, body, cancellationToken);

    public Task<ApiResponse<Product>> UpdateAsync(
        int productId,
        ProductRequest body,
        CancellationToken cancellationToken = default) =>
        PutJsonAsync<Product>(ById(productId), body, cancellationToken);

    public Task<ApiResponse<Product>> DeleteAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        DeleteAsync<Product>(ById(productId), cancellationToken);

    private static string ById(int productId) => $"{ProductsResource}/{productId}";
}
