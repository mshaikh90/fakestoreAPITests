using FakeStoreApi.Core.Configuration;
using FakeStoreApi.Core.Http;
using FakeStoreApi.Core.Models;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace FakeStoreApi.Core.Clients;

public sealed class CartsClient : ApiClientBase
{
    private const string CartsResource = "/carts";

    public CartsClient()
    {
    }

    public CartsClient(ApiClientOptions options, ILoggerFactory? loggerFactory = null)
        : base(options, loggerFactory)
    {
    }

    public CartsClient(RestClient client)
        : base(client)
    {
    }

    public Task<ApiResponse<List<Cart>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<Cart>>(CartsResource, cancellationToken);

    public Task<ApiResponse<Cart>> GetByIdAsync(
        int cartId,
        CancellationToken cancellationToken = default) =>
        GetAsync<Cart>(ById(cartId), cancellationToken);

    public Task<ApiResponse<Cart>> CreateAsync(
        CreateCartRequest body,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<Cart>(CartsResource, body, cancellationToken);

    public Task<ApiResponse<Cart>> UpdateAsync(
        int cartId,
        CreateCartRequest body,
        CancellationToken cancellationToken = default) =>
        PutJsonAsync<Cart>(ById(cartId), body, cancellationToken);

    public Task<ApiResponse<Cart>> DeleteAsync(
        int cartId,
        CancellationToken cancellationToken = default) =>
        DeleteAsync<Cart>(ById(cartId), cancellationToken);

    private static string ById(int cartId) => $"{CartsResource}/{cartId}";
}
