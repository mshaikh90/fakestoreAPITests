# FakeStore API Test Framework

A lightweight C# test framework that exercises the [FakeStore](https://fakestoreapi.com) REST API. Built on **NUnit**, **RestSharp**, and **ExtentReports**, with parallel fixture execution, auto-retry on transient failures, narrated test steps, and HTML reports out of the box.

For a deep walkthrough of the architecture, read [FRAMEWORK_OVERVIEW.md](FRAMEWORK_OVERVIEW.md). This README is for **getting it running** and **writing new tests**.

---

## Prerequisites

| Requirement | Version | Why |
|---|---|---|
| **.NET SDK** | 10.0 or newer | Both projects target `net10.0`. |
| **Internet connection** | — | The tests hit `https://fakestoreapi.com`. |
| **IDE (optional)** | JetBrains Rider, Visual Studio 2022+, or VS Code with the C# Dev Kit | Any of these gives you a graphical test explorer. |

Verify your .NET version:

```bash
dotnet --version
```

If the output is below `10.0.x`, install the latest SDK from <https://dotnet.microsoft.com/download>.

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/mshaikh90/fakestoreAPITests.git
cd C#RestSharpNUnit
```

### 2. Restore dependencies

```bash
dotnet restore
```

This downloads every NuGet package listed in the project files (`RestSharp`, `NUnit`, `ExtentReports`, `Polly`, `Microsoft.Extensions.Logging.Console`).

### 3. Build the solution

```bash
dotnet build
```

A successful build prints `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

### 4. Run the tests

```bash
# Run everything
dotnet test

# Run a category
dotnet test --filter Category=smoke
dotnet test --filter Category=Product
dotnet test --filter Category=Cart

# Run a single test by name
dotnet test --filter Name=ProductsEndpoint_ReturnsAtLeastOneProduct
```

### 5. Open the HTML report

After every run, an Extent HTML report is written to:

```
TestResults/ExtentReports/{yyyyMMdd_HHmmss}/index.html
```

The console also prints the path. Open it in any browser to see passing/failing tests, narrated steps, and the captured HTTP transcript for any failures.

---

## Configuration

The framework reads its settings in this priority order (highest first):

1. **Environment variables** — useful for CI:
   - `API_BASE_URL`
   - `API_TIMEOUT_SECONDS`
   - `API_ENABLE_HTTP_LOGGING`, `API_CAPTURE_HTTP_TRANSCRIPT`
   - `API_LOG_REQUEST_BODY`, `API_LOG_RESPONSE_BODY`
   - `API_ENABLE_RETRY`, `API_MAX_RETRY_ATTEMPTS`, `API_RETRY_BASE_DELAY_MS`

2. **`appsettings.json`** — at `tests/FakeStoreApi.Tests/appsettings.json`. Edit this for local development.

3. **Hard-coded defaults** — last-resort fallback in `ApiClientOptions`.

To point the suite at a different environment, edit one line of `appsettings.json`:

```json
{
  "ApiClient": {
    "BaseUrl": "https://staging.fakestoreapi.com/"
  }
}
```

Or override per-run:

```bash
API_BASE_URL=https://staging.fakestoreapi.com/ dotnet test
```

---

## Writing a new test

### The 30-second version

1. Open or create a file under `tests/FakeStoreApi.Tests/Tests/`.
2. Make the class inherit from `ApiTestBase`.
3. Mark the class with `[TestFixture]` and one or more `[Category(...)]` attributes.
4. Add an `async Task` method, mark it with `[Test]`, and write the scenario.

### Step-by-step walkthrough

Suppose you want to verify that fetching a known product by ID returns its title. Here's a working example, top to bottom:

```csharp
using System.Net;
using FakeStoreApi.Tests.Support;

namespace FakeStoreApi.Tests.Tests;

[TestFixture]                    // Tells NUnit this class contains tests
[Category("Product")]            // Lets us filter with --filter Category=Product
public sealed class GetProductByIdTests : ApiTestBase   // Inherit from the base
{
    [Test]                       // Marks this method as a runnable test
    public async Task GetProductById_ReturnsExpectedTitle()
    {
        // 1. Narrate what's about to happen.
        //    The text appears in the console AND the Extent HTML report,
        //    so anyone reading either can follow the test without reading C#.
        Scenario.Step("Fetch product with ID 1");

        // 2. Call the API. Products is a ProductsClient pre-built in [SetUp].
        //    GetByIdAsync returns Task<ApiResponse<Product>>.
        //    'await' waits for the call to finish.
        var response = await Products.GetByIdAsync(1);

        // 3. Assert. ShouldHaveData checks the status code AND that the
        //    response body deserialised. It returns the unwrapped Product.
        var product = response.ShouldHaveData(HttpStatusCode.OK);

        // 4. Make scenario-specific assertions on the unwrapped object.
        Scenario.Step("Verify the product has the expected title");
        Assert.That(product.Title, Does.Contain("Backpack"),
            "Product 1 should contain 'Backpack' in its title.");
    }
}
```

That's a complete test. Run it with:

```bash
dotnet test --filter Name=GetProductById_ReturnsExpectedTitle
```

### The patterns to know

The framework gives you a small set of building blocks. Use them and the test stays clean.

#### `ApiTestBase`

Every test fixture inherits from this. It hands you two ready-to-use clients:

```csharp
protected ProductsClient Products { get; }
protected CartsClient Carts { get; }
```

Each client exposes the full set of HTTP operations: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.

#### `Scenario.Step("...")`

Call this before each logical phase of the test. It writes to the console **and** to the HTML report. **Use it generously** — it's the single biggest readability win.

```csharp
Scenario.Step("Fetch all products from the API");
Scenario.Step("Filter to the cheapest electronics product");
Scenario.Step("Create a cart containing that product");
```

#### `ShouldHaveData` / `ShouldHaveStatus`

Two extension methods on `ApiResponse<T>` — defined in `Support/ApiResponseAssertions.cs`.

```csharp
// Most common: status check + null check + return the unwrapped data
var products = (await Products.GetAllAsync()).ShouldHaveData(HttpStatusCode.OK);

// When you only care about the status (e.g. asserting a 404)
response.ShouldHaveStatus(HttpStatusCode.NotFound);
```

The operation name (`GET /products`) is auto-derived from the response, so failure messages are descriptive without you typing the URL.

#### Query helpers (`ProductQueries`, `CartQueries`)

Hide LINQ behind named methods so the test reads as prose:

```csharp
var cheapest = products.CheapestInCategory("electronics");
var lowestRated = products.LowestRated();
var highestId = carts.HighestId();
```

If you find yourself writing `.Where(...).OrderBy(...).FirstOrDefault()` inline, add a new method to `ProductQueries.cs` or `CartQueries.cs` instead.

#### Builders (`CartBuilder`)

Hide multi-line object construction behind a one-liner:

```csharp
var request = CartBuilder.WithProduct(productId);
// instead of:
// var request = new CreateCartRequest { UserId = 1, Date = ..., Products = [...] };
```

Add a new builder when a request body has more than three properties or appears in more than one test.

#### `TestContext.Out.WriteLine(...)`

Use this for diagnostic info that's useful in a *failed* test transcript but doesn't fit a step description:

```csharp
TestContext.Out.WriteLine($"Cheapest electronics: [{cheapest.Id}] {cheapest.Title} @ ${cheapest.Price}");
```

#### `Assert.Multiple { ... }`

Use this when several independent assertions check different properties of the *same* object. NUnit reports all failures at once instead of stopping at the first.

```csharp
Assert.Multiple(() =>
{
    Assert.That(product.Id, Is.GreaterThan(0));
    Assert.That(product.Title, Is.Not.Empty);
    Assert.That(product.Price, Is.GreaterThan(0m));
});
```

### Categories

Apply at class level (covers every test) or method level (one test only):

```csharp
[TestFixture]
[Category("Product")]                // every test in the class
public sealed class ProductTests : ApiTestBase
{
    [Test]
    [Category("smoke")]              // additionally tagged smoke
    public async Task FastSanityCheck() { ... }
}
```

Conventions in this codebase:

| Category | Meaning |
|---|---|
| `smoke` | Fast, reliable, runs on every commit |
| `Product`, `Cart` | Feature area |
| `mock-quirk` | Tests that demonstrate a FakeStore mock-API limitation; usually combined with `[Explicit]` |

### Marking a test as opt-in only

If a test demonstrates a known limitation (e.g. a mock-API quirk) and you don't want it in the default run, mark it `[Explicit]` with a one-line description:

```csharp
[Test]
[Explicit("FakeStore mock does not persist POSTed carts; GET /carts/{newId} returns null.")]
public async Task ExpectedToFailAgainstMock() { ... }
```

It will be skipped by `dotnet test` but can be invoked with `dotnet test --filter Category=mock-quirk` (if you also tag it that way) or directly by name.

### Adding test data for parameterised tests

Drop static factory methods into `tests/FakeStoreApi.Tests/TestData/`:

```csharp
public static class ProductTestData
{
    public static IEnumerable<TestCaseData> ValidProductRequests()
    {
        yield return new TestCaseData(new ProductRequest { ... }).SetName("Create - widget A");
        yield return new TestCaseData(new ProductRequest { ... }).SetName("Create - widget B");
    }
}
```

Then drive a test off it:

```csharp
[TestCaseSource(typeof(ProductTestData), nameof(ProductTestData.ValidProductRequests))]
public async Task CreateProduct_Works(ProductRequest request) { ... }
```

NUnit runs the test once per row, with the `SetName(...)` text appearing in the test explorer.

### Adding a new client (when a new resource lands)

1. Add a model class in `src/FakeStoreApi.Core/Models/` with `[JsonPropertyName(...)]` attributes.
2. Create `src/FakeStoreApi.Core/Clients/{Resource}sClient.cs` inheriting from `ApiClientBase`. Wrap each endpoint as a one-line method:
   ```csharp
   public Task<ApiResponse<List<Order>>> GetAllAsync(...) =>
       GetAsync<List<Order>>("/orders", ...);
   ```
3. Expose it on `ApiTestBase`:
   ```csharp
   protected OrdersClient Orders { get; private set; } = null!;
   ```
4. Initialise in `CreateClients()` alongside `Products` and `Carts`.

Now every test can call `Orders.GetAllAsync()` with zero plumbing.

### Checklist before opening a PR

- [ ] Test class inherits from `ApiTestBase`
- [ ] Class has `[TestFixture]` and at least one `[Category(...)]`
- [ ] Each test method has `[Test]` (or `[TestCaseSource(...)]`)
- [ ] Scenario steps narrate every logical phase of the test
- [ ] Assertions use `ShouldHaveData` / `ShouldHaveStatus` instead of inspecting raw responses
- [ ] LINQ chains live in `ProductQueries`/`CartQueries`, not in the test body
- [ ] Multi-property request objects come from a builder, not inline
- [ ] `dotnet build` is clean (zero warnings, zero errors)
- [ ] `dotnet test --filter Name=YourNewTest` passes locally

---

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `dotnet test` finds zero tests | Run `dotnet build` first; the test discoverer needs the compiled DLL. |
| Tests appear under `ExtentReportRunHooks` in the test explorer | Reload the test discoverer; the `[SetUpFixture]` is in the global namespace, so this shouldn't happen on a clean build. |
| HTML report not generated | Check the console output for the `Extent report:` path. The directory is created on first run; ensure you have write permission to `TestResults/`. |
| Network failures cause tests to flake | The framework auto-retries on 5xx and network errors via Polly. If you're seeing 4xx flakes, that's a real test failure — investigate, don't retry. |
| All tests run sequentially | The framework runs fixtures in parallel via `[assembly: Parallelizable(ParallelScope.Fixtures)]`. If you've removed it, add it back to `AssemblyAttributes.cs`. |
