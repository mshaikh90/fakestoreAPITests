using System.Text.Json.Serialization;

namespace FakeStoreApi.Core.Models;

public sealed class CartProductItem
{
    [JsonPropertyName("productId")]
    public int ProductId { get; init; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }
}
