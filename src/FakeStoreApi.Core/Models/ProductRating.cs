using System.Text.Json.Serialization;

namespace FakeStoreApi.Core.Models;

public sealed class ProductRating
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
