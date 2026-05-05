using FakeStoreApi.Core.Clients;
using FakeStoreApi.Core.Configuration;

namespace FakeStoreApi.Tests.Support;

/// <summary>
/// Base class for all API tests. Inheriting from this gives you <see cref="Products"/>
/// and <see cref="Carts"/> pre-wired to the configured base URL.
///
/// To point tests at a different environment, override <see cref="GetOptions"/> in your
/// subclass and return a custom <see cref="ApiClientOptions"/> — no other changes needed.
/// </summary>
public abstract class ApiTestBase
{
    protected ProductsClient Products { get; private set; } = null!;

    protected CartsClient Carts { get; private set; } = null!;

    /// <summary>
    /// Override this in a subclass to supply different options (e.g. a different base URL
    /// for a staging environment). The default uses <c>appsettings.json</c> / env vars.
    /// </summary>
    protected virtual ApiClientOptions GetOptions() => ApiClientOptions.FromConfiguration();

    [SetUp]
    public void CreateClients()
    {
        var options = GetOptions();
        Products = new ProductsClient(options);
        Carts    = new CartsClient(options);
    }
}
