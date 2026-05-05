using FakeStoreApi.Core.Models;

namespace FakeStoreApi.Tests.Support;

/// <summary>
/// Reusable assertions for <see cref="Product"/> responses.
/// Call these from any test class instead of repeating the individual Assert.That lines.
/// </summary>
public static class ProductAssertions
{
    /// <summary>
    /// Asserts that every field on <paramref name="actual"/> matches the values
    /// that were submitted in <paramref name="expected"/>.
    /// </summary>
    public static void ShouldMatchRequest(this Product actual, ProductRequest expected, string operation)
    {
        Assert.That(actual.Title,       Is.EqualTo(expected.Title),       $"{operation} returned the wrong title.");
        Assert.That(actual.Price,       Is.EqualTo(expected.Price),       $"{operation} returned the wrong price.");
        Assert.That(actual.Description, Is.EqualTo(expected.Description), $"{operation} returned the wrong description.");
        Assert.That(actual.Category,    Is.EqualTo(expected.Category),    $"{operation} returned the wrong category.");
        Assert.That(actual.Image,       Is.EqualTo(expected.Image),       $"{operation} returned the wrong image.");
    }
}

