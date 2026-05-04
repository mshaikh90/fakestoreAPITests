using FakeStoreApi.Core.Clients;

namespace FakeStoreApi.Tests.Support;

public abstract class ApiTestBase
{
    protected ProductsClient Products { get; private set; } = null!;

    protected CartsClient Carts { get; private set; } = null!;

    [SetUp]
    public void CreateClients()
    {
        Products = new ProductsClient();
        Carts = new CartsClient();
    }
}
