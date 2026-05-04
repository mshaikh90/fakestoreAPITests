using System.Text.Json.Serialization;

namespace FakeStoreApi.Core.Models;

public sealed class Cart
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("products")]
    public IReadOnlyList<CartProductItem> Products { get; init; } = [];
}
