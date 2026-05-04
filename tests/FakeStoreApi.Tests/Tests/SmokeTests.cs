using System.Net;
using FakeStoreApi.Tests.Support;

namespace FakeStoreApi.Tests.Tests;

/// <summary>
/// Fast, reliable checks that the API is reachable and returning valid data shapes.
/// Run on every commit. If these fail, the test environment itself is broken.
/// </summary>
[TestFixture]
[Category("smoke")]
public sealed class SmokeTests : ApiTestBase
{
    [Test]
    public async Task ProductsEndpoint_ReturnsAtLeastOneProduct()
    {
        Scenario.Step("Call GET /products and confirm the API returns a non-empty list");

        var products = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(products, Is.Not.Empty, "GET /products returned an empty list.");
    }

    [Test]
    public async Task CartsEndpoint_ReturnsAtLeastOneCart()
    {
        Scenario.Step("Call GET /carts and confirm the API returns a non-empty list");

        var carts = (await Carts.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(carts, Is.Not.Empty, "GET /carts returned an empty list.");
    }

    [Test]
    public async Task ProductsEndpoint_ReturnsAtLeastOneProductPerExpectedCategory()
    {
        Scenario.Step("Call GET /products");
        var products = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Confirm each expected category has at least one product");
        string[] expectedCategories = ["electronics", "jewelery", "men's clothing", "women's clothing"];

        Assert.Multiple(() =>
        {
            foreach (var category in expectedCategories)
            {
                Assert.That(products.InCategory(category), Is.Not.Empty,
                    $"No products returned for category '{category}'.");
            }
        });
    }
}
