using FakeStoreApi.Core.Models;

namespace FakeStoreApi.Tests.Support;

/// <summary>
/// Named queries over a cart list. Keeps tests focused on the scenario while
/// common ID-selection logic lives behind intention-revealing method names.
/// </summary>
public static class CartQueries
{
    public static int HighestId(this IEnumerable<Cart> carts) =>
        carts.Max(cart => cart.Id);
}
