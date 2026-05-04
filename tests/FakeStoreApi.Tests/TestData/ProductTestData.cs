using FakeStoreApi.Core.Models;
using NUnit.Framework;

namespace FakeStoreApi.Tests.TestData;

public static class ProductTestData
{
    public static IReadOnlyList<ProductRequest> ProductsToCreate() =>
    [
        new ProductRequest
        {
            Title = "404 T-Shirt",
            Price = 49.99m,
            Description = "A T-shirt you will buy, but won't be able to find again.",
            Category = "men's clothing",
            Image = "https://example.com/images/404-not-found-t-shirt.png"
        },
        new ProductRequest
        {
            Title = "API Test Runner Hoodie",
            Price = 34.5m,
            Description = "Comfortable hoodie for long regression test runs.",
            Category = "men's clothing",
            Image = "https://example.com/images/api-test-runner-hoodie.png"
        },
        new ProductRequest
        {
            Title = "500 internal service error hoodie",
            Price = 15.25m,
            Description = "A hoodie that might mess up your washing machine.",
            Category = "men's clothing",
            Image = "https://example.com/images/500-internal-service-error-hoodie.png"
        }
    ];

    public static IEnumerable<TestCaseData> ValidProductRequests()
    {
        foreach (var product in ProductsToCreate())
            yield return new TestCaseData(product).SetName($"Create product - {product.Title}");
    }
}
