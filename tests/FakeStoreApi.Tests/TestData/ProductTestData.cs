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

    /// <summary>
    /// A ready-made request used by tests that only need to verify ID uniqueness.
    /// Using a named method keeps test bodies free of boilerplate object initialisation.
    /// </summary>
    public static ProductRequest UniqueIdVerificationProduct() => new()
    {
        Title       = "Unique ID Verification Product",
        Price       = 19.99m,
        Description = "Used to verify that POST /products returns a freshly assigned ID.",
        Category    = "electronics",
        Image       = "https://example.com/images/unique-id-verification-product.png"
    };
}
