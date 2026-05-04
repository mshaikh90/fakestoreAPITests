using System.Net;
using FakeStoreApi.Tests.Support;

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
}
