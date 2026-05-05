using System.Net;
using FakeStoreApi.Core.Models;
using FakeStoreApi.Tests.Support;
using FakeStoreApi.Tests.TestData;

namespace FakeStoreApi.Tests.Tests;

[TestFixture]
[Category("Cart")]
public sealed class CartTests : ApiTestBase
{
    [Test]
    public async Task GetCart_WithNonExistentCartId_Returns404WithErrorMessage()
    {
        Scenario.Step("Fetch all carts from the API");
        var carts = (await Carts.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(carts, Is.Not.Empty, "GET /carts returned no carts.");

        Scenario.Step("Calculate a cart ID that is not present in the returned list");
        var highestCartId = carts.HighestId();
        var nonExistentCartId = highestCartId + 1;

        TestContext.Out.WriteLine(
            $"Highest cart ID returned by GET /carts: {highestCartId}. Requesting non-existent ID: {nonExistentCartId}");

        Scenario.Step("Request the non-existent cart by ID");
        var missingCartResponse = await Carts.GetByIdAsync(nonExistentCartId);
        var errorBody = missingCartResponse.RawContent ?? string.Empty;

        Scenario.Step("Verify the API returns a not-found response with a useful error body");
        Assert.Multiple(() =>
        {
            missingCartResponse.ShouldHaveStatus(HttpStatusCode.NotFound);

            Assert.That(errorBody, Is.Not.Empty,
                $"Expected {missingCartResponse.Operation} to return an error message body.");

            Assert.That(errorBody.ToLowerInvariant(), Does.Contain("not found"),
                $"Expected {missingCartResponse.Operation} error body to explain that the cart was not found.");
        });
    }

    [Test]
    public async Task CreateCart_WithOneProduct_ReturnsNewCartId()
    {
        Scenario.Step("Build a cart request containing one product");
        var request = CartBuilder.WithProduct(productId: 1, quantity: 2);

        Scenario.Step("Submit the cart to the API");
        var createResponse = await Carts.CreateAsync(request);
        var createdCart = createResponse.ShouldHaveData(HttpStatusCode.Created);

        Scenario.Step("Verify the response contains a valid cart ID and the correct user");
        Assert.Multiple(() =>
        {
            Assert.That(createdCart.Id,     Is.GreaterThan(0),              $"{createResponse.Operation} did not return a valid cart ID.");
            Assert.That(createdCart.UserId, Is.EqualTo(request.UserId),     $"{createResponse.Operation} returned the wrong user ID.");
        });

        TestContext.Out.WriteLine($"Cart created with ID: {createdCart.Id}");
    }

    [Test]
    public async Task UpdateCart_ChangesProductQuantity()
    {
        Scenario.Step("Create an initial cart with one product");
        var created = (await Carts.CreateAsync(CartBuilder.WithProduct(productId: 1, quantity: 1)))
            .ShouldHaveData(HttpStatusCode.Created);

        TestContext.Out.WriteLine($"Created cart ID: {created.Id}");

        Scenario.Step("Build an update request with a different quantity");
        var updateRequest = CartBuilder.WithProduct(productId: 1, quantity: 5);

        Scenario.Step("Send the update");
        var updateResponse = await Carts.UpdateAsync(created.Id, updateRequest);
        var updatedCart = updateResponse.ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Verify the returned cart reflects the updated quantity");
        Assert.That(updatedCart.Products[0].Quantity, Is.EqualTo(5),
            $"{updateResponse.Operation} did not apply the updated quantity.");
        
        Scenario.Step("Fetch cart to confirm change persisted");
        var cartResponse = await Carts.GetByIdAsync(created.Id);
        var fetchedCart = cartResponse.ShouldHaveData(HttpStatusCode.OK);
        Assert.That(fetchedCart.Products[0].Quantity, Is.EqualTo(5),
            $"GET /carts/{created.Id} did not reflect the updated quantity.");  
    }

    [Test]
    public async Task DeleteCart_ReturnsDeletedCart()
    {
        Scenario.Step("Fetch all existing carts and pick the one with the highest ID");
        var carts = (await Carts.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(carts, Is.Not.Empty, "GET /carts returned no carts.");

        var targetId = carts.HighestId();
        TestContext.Out.WriteLine($"Deleting cart ID: {targetId}");

        Scenario.Step("Delete the cart");
        var deleteResponse = await Carts.DeleteAsync(targetId);
        var deletedCart = deleteResponse.ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Verify the response echoes back the same cart ID");
        Assert.That(deletedCart.Id, Is.EqualTo(targetId),
            $"{deleteResponse.Operation} returned the wrong cart ID.");
    }
}
