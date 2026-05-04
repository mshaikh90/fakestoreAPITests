using FakeStoreApi.Core.Models;

namespace FakeStoreApi.Tests.Support;

/// <summary>
/// Named queries over a product list. Each method has an English name that says
/// what it returns, hiding the LINQ behind a readable call site.
/// </summary>
public static class ProductQueries
{
    public static IEnumerable<Product> InCategory(this IEnumerable<Product> products, string category) =>
        products.Where(product =>
            product.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    public static Product? CheapestInCategory(this IEnumerable<Product> products, string category) =>
        products.InCategory(category)
            .OrderBy(product => product.Price)
            .FirstOrDefault();

    public static Product? MostExpensiveInCategory(this IEnumerable<Product> products, string category) =>
        products.InCategory(category)
            .OrderByDescending(product => product.Price)
            .FirstOrDefault();

    public static Product? LowestRated(this IEnumerable<Product> products) =>
        products.Where(product => product.Rating is not null)
            .OrderBy(product => product.Rating!.Rate)
            .ThenBy(product => product.Id)
            .FirstOrDefault();

    public static Product? HighestRated(this IEnumerable<Product> products) =>
        products.Where(product => product.Rating is not null)
            .OrderByDescending(product => product.Rating!.Rate)
            .ThenBy(product => product.Id)
            .FirstOrDefault();
}
