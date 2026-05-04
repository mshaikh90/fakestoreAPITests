using FakeStoreApi.Core.Models;

namespace FakeStoreApi.Tests.TestData;

/// <summary>
/// Factory for <see cref="CreateCartRequest"/>. Hides the boilerplate of cart construction
/// (date formatting, default user, list-of-one initialisation) so tests can express intent
/// in a single line.
/// </summary>
public static class CartBuilder
{
    public const int DefaultUserId = 1;

    public static CreateCartRequest WithProduct(int productId, int quantity = 1, int userId = DefaultUserId) =>
        new()
        {
            UserId = userId,
            Date = Today(),
            Products = [new CartProductItem { ProductId = productId, Quantity = quantity }]
        };

    public static CreateCartRequest WithProducts(
        IEnumerable<CartProductItem> products,
        int userId = DefaultUserId) =>
        new()
        {
            UserId = userId,
            Date = Today(),
            Products = products.ToList()
        };

    private static string Today() => DateTime.Today.ToString("yyyy-MM-dd");
}
