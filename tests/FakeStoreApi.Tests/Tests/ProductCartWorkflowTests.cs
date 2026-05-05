using System.Net;
using FakeStoreApi.Core.Models;
using FakeStoreApi.Tests.Support;
using FakeStoreApi.Tests.TestData;

namespace FakeStoreApi.Tests.Tests;

[TestFixture]
[Category("Product")]
[Category("Cart")]
public sealed class ProductCartWorkflowTests : ApiTestBase
{
    [Test]
    public async Task CheapestElectronicsProduct_IsAddedToCart_AndCartIsCreated()
    {
        Scenario.Step("Fetch all products from the API");
        var products = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);
        Assert.That(products, Is.Not.Empty, "GET /products returned no products.");

        Scenario.Step("Find the cheapest product in the 'electronics' category");
        var cheapest = products.CheapestInCategory("electronics");
        Assert.That(cheapest, Is.Not.Null, "No electronics products found.");
        TestContext.Out.WriteLine(
            $"Cheapest electronics: [{cheapest!.Id}] {cheapest.Title} @ ${cheapest.Price}");

        Scenario.Step("Create a new cart containing that product");
        var createdCart = (await Carts.CreateAsync(CartBuilder.WithProduct(cheapest.Id)))
            .ShouldHaveData(HttpStatusCode.Created);
        Assert.That(createdCart.Id, Is.GreaterThan(0), "Cart creation did not return a valid ID.");
        TestContext.Out.WriteLine($"Cart created with ID: {createdCart.Id}");

        Scenario.Step("Verify the cart can be retrieved by its ID");
        var retrievedCart = (await Carts.GetByIdAsync(createdCart.Id))
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(retrievedCart.Products, Has.Count.EqualTo(1),
            "Expected exactly 1 product in the retrieved cart.");
        Assert.That(retrievedCart.Products[0].ProductId, Is.EqualTo(cheapest.Id),
            "Retrieved cart does not contain the expected product.");
    }
}
