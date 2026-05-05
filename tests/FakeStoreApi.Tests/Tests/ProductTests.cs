using System.Net;
using FakeStoreApi.Core.Models;
using FakeStoreApi.Tests.Support;
using FakeStoreApi.Tests.TestData;

namespace FakeStoreApi.Tests.Tests;

[TestFixture]
[Category("Product")]
public sealed class ProductTests : ApiTestBase
{
    [TestCaseSource(typeof(ProductTestData), nameof(ProductTestData.ValidProductRequests))]
    public async Task CreateProduct_CanBeRetrievedByReturnedId_WithMatchingDetails(ProductRequest request)
    {
        Scenario.Step("Create a product using the test case data");
        var createResponse = await Products.CreateAsync(request);
        var createdProduct = createResponse.ShouldHaveData(HttpStatusCode.Created);

        Scenario.Step("Verify the create response contains the submitted product details");
        Assert.Multiple(() =>
        {
            Assert.That(createdProduct.Id, Is.GreaterThan(0),
                $"{createResponse.Operation} did not return a valid ID for '{request.Title}'.");

            createdProduct.ShouldMatchRequest(request, createResponse.Operation);
        });

        TestContext.Out.WriteLine($"Created '{request.Title}' with returned ID: {createdProduct.Id}");

        Scenario.Step("Retrieve the product by the ID returned from the create response");
        var retrievedResponse = await Products.GetByIdAsync(createdProduct.Id);
        var retrievedProduct = retrievedResponse.ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Verify the retrieved product matches the original request");
        Assert.Multiple(() =>
        {
            Assert.That(retrievedProduct.Id, Is.EqualTo(createdProduct.Id),
                $"{retrievedResponse.Operation} returned the wrong product ID.");

            retrievedProduct.ShouldMatchRequest(request, retrievedResponse.Operation);
        });
    }

    [Test]
    public async Task DeleteLowestRatedProduct_RemovesProductFromGetByIdAndProductList()
    {
        Scenario.Step("Fetch all products from the API");
        var products = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(products, Is.Not.Empty, "GET /products returned no products.");

        Scenario.Step("Select the product with the lowest rating");
        var productToDelete = products.LowestRated();

        Assert.That(productToDelete, Is.Not.Null, "GET /products returned no products with ratings.");
        var productId = productToDelete!.Id;

        TestContext.Out.WriteLine(
            $"Lowest rated product: [{productId}] {productToDelete.Title} with rating {productToDelete.Rating!.Rate}");

        Scenario.Step("Delete the selected product");
        var deleteResponse = await Products.DeleteAsync(productId);
        var deletedProduct = deleteResponse.ShouldHaveData(HttpStatusCode.OK);

        Assert.That(deletedProduct.Id, Is.EqualTo(productId),
            $"{deleteResponse.Operation} returned the wrong product ID.");

        Scenario.Step("Request the deleted product by ID");
        var getDeletedProductResponse = await Products.GetByIdAsync(productId);

        Scenario.Step("Fetch all products again after the delete");
        var productsAfterDeleteResponse = await Products.GetAllAsync();
        var productsAfterDelete = productsAfterDeleteResponse.ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Verify the deleted product can no longer be retrieved or listed");
        Assert.Multiple(() =>
        {
            getDeletedProductResponse.ShouldHaveStatus(HttpStatusCode.NotFound);

            Assert.That(getDeletedProductResponse.RawContent, Is.Not.Null.And.Not.Empty,
                $"{getDeletedProductResponse.Operation} should return an error response body.");

            Assert.That(productsAfterDelete.Select(product => product.Id), Does.Not.Contain(productId),
                $"{productsAfterDeleteResponse.Operation} after delete still returned product ID {productId}.");
        });
    }

      [Test]
    public async Task DeleteHighestRatedProduct_RemovesProductFromGetByIdAndProductList()
    {
        Scenario.Step("Fetch all products from the API");
        var products = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(products, Is.Not.Empty, "GET /products returned no products.");

        Scenario.Step("Select the product with the highest rating");
        var productToDelete = products.HighestRated();

        Assert.That(productToDelete, Is.Not.Null, "GET /products returned no products with ratings.");
        var productId = productToDelete!.Id;

        TestContext.Out.WriteLine(
            $"Lowest rated product: [{productId}] {productToDelete.Title} with rating {productToDelete.Rating!.Rate}");

        Scenario.Step("Delete the selected product");
        var deleteResponse = await Products.DeleteAsync(productId);
        var deletedProduct = deleteResponse.ShouldHaveData(HttpStatusCode.OK);

        Assert.That(deletedProduct.Id, Is.EqualTo(productId),
            $"{deleteResponse.Operation} returned the wrong product ID.");

        Scenario.Step("Request the deleted product by ID");
        var getDeletedProductResponse = await Products.GetByIdAsync(productId);

        Scenario.Step("Fetch all products again after the delete");
        var productsAfterDeleteResponse = await Products.GetAllAsync();
        var productsAfterDelete = productsAfterDeleteResponse.ShouldHaveData(HttpStatusCode.OK);

        Scenario.Step("Verify the deleted product can no longer be retrieved or listed");
        Assert.Multiple(() =>
        {
            getDeletedProductResponse.ShouldHaveStatus(HttpStatusCode.NotFound);

            Assert.That(getDeletedProductResponse.RawContent, Is.Not.Null.And.Not.Empty,
                $"{getDeletedProductResponse.Operation} should return an error response body.");

            Assert.That(productsAfterDelete.Select(product => product.Id), Does.Not.Contain(productId),
                $"{productsAfterDeleteResponse.Operation} after delete still returned product ID {productId}.");
        });
    }

    [Test]
    public async Task GeneratedProductIdDoesNotCurrentlyExist()
    {
        Scenario.Step("Fetch all existing products");
        var existingProducts = (await Products.GetAllAsync())
            .ShouldHaveData(HttpStatusCode.OK);

        Assert.That(existingProducts, Is.Not.Empty, "GET /products returned no products.");

        Scenario.Step("Capture the IDs of all existing products");
        var existingProductIds = existingProducts.Select(product => product.Id).ToArray();
        TestContext.Out.WriteLine($"Existing product IDs: [{string.Join(", ", existingProductIds)}]");

        Scenario.Step("Create a new product");
        var newProductRequest = ProductTestData.UniqueIdVerificationProduct();
        var createResponse = await Products.CreateAsync(newProductRequest);
        var createdProduct = createResponse.ShouldHaveData(HttpStatusCode.Created);

        TestContext.Out.WriteLine($"New product created with ID: {createdProduct.Id}");

        Scenario.Step("Verify the new product ID is not present in the previously captured list");
        Assert.That(existingProductIds, Does.Not.Contain(createdProduct.Id),
            $"{createResponse.Operation} returned ID {createdProduct.Id}, which already exists in the product list.");
    }
}
