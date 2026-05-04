# How This Test Framework Is Built

## What it does

This is an automated API test framework. Its job is to call the [FakeStore](https://fakestoreapi.com) REST API, check that the responses are correct, and produce a clear, shareable report of what happened.

It's written in C# on .NET 10, with three main libraries doing the heavy lifting:

- **NUnit** — the test runner; it discovers and executes test methods.
- **RestSharp** — the HTTP client; it makes the actual network calls.
- **ExtentReports** — generates a polished HTML report at the end of every run.

The framework is built around three principles:

1. **Lightweight** — only build what's needed; no speculative abstractions.
2. **Separation of concerns** — each file does one job and does it clearly.
3. **Readable tests** — someone with limited C# knowledge should be able to read a test and understand it.

---

## The big picture

The solution has two projects:

```
C#RestSharpNUnit/
├── src/
│   └── FakeStoreApi.Core/       ← reusable library (the "framework")
└── tests/
    └── FakeStoreApi.Tests/      ← actual tests + test-specific helpers
```

The split exists because they answer different questions:

- **Core** answers: *how do we talk to the API and turn HTTP responses into typed C# objects?*
- **Tests** answers: *what scenarios are we verifying?*

Anyone could take the Core library and write a completely different test suite against the same API. That's the whole point of separating them.

---

## What happens when a test runs

Trace this single line of test code:

```csharp
var products = (await Products.GetAllAsync()).ShouldHaveData(HttpStatusCode.OK);
```

Step by step, here's what happens:

1. **`Products`** is a `ProductsClient` — a service class that knows the URL paths for the products endpoints. It was created automatically in `[SetUp]` from the test's base class.

2. **`GetAllAsync()`** asks the client to issue an HTTP `GET /products` request. The `Async` suffix means the call returns a `Task<T>` — a "promise to deliver a result later." The `await` keyword pauses the test until that result arrives.

3. The request travels through a chain of small HTTP handlers, each doing one job:
   - **Logging handler** — writes `GET https://fakestoreapi.com/products` to the console.
   - **Transcript handler** — saves the request and response in an in-memory log scoped to this test.
   - **Resilience handler** — if the server returns a 500 error or the network blips, it automatically retries.
   - Then the actual network call goes out.

4. RestSharp sends the request, gets JSON back, and converts it into a `List<Product>` — a list of typed C# objects.

5. The framework wraps the result in our own `ApiResponse<T>` object, which carries the data, status code, URL, and headers all together.

6. **`.ShouldHaveData(HttpStatusCode.OK)`** is an assertion. It checks two things:
   - Did the API return `200 OK`? If not, fail with a clear message including the response body.
   - Did the response actually contain a body? If not, fail with "did not return a response body."

7. If both checks pass, `ShouldHaveData` returns the unwrapped list. So `products` is now a `List<Product>` we can filter, sort, and inspect.

That's all from one line. The test then continues with more steps and assertions.

---

## The Core library, piece by piece

### `Configuration/ApiClientOptions.cs`

A single record (a special kind of immutable C# class) holding every setting the framework cares about: base URL, timeout, retry behaviour, logging flags. It's loaded once at startup, in **priority order**:

1. **Environment variables** (highest priority — used by CI pipelines)
2. **`appsettings.json`** in the test project (used by developers running locally)
3. **Hard-coded defaults** in the record itself (the safety net)

**Why this way?** Production-grade frameworks need to be reconfigurable without code changes. A developer edits `appsettings.json`. A CI pipeline sets `API_BASE_URL=https://staging.example.com`. Neither requires a recompile. This is the "environment is set in one place" requirement.

### `Models/`

Plain C# classes that mirror the JSON shapes the API returns:

- `Product` — a single product
- `Cart`, `CartProductItem` — a cart and its line items
- `ProductRequest`, `CreateCartRequest` — the bodies we send when creating things
- `ProductRating` — a nested object on `Product`

Each property has a `[JsonPropertyName("id")]` attribute so the JSON maps cleanly to the C# property name. These are dumb data containers — no logic, just shape.

### `Http/ApiResponse.cs`

A wrapper around RestSharp's response. We have our own version because:

1. **It hides RestSharp from the rest of the code.** If we ever swap RestSharp for a different HTTP library (HttpClient, Refit, Flurl), only this file and `ApiClientBase` change — the tests don't.
2. **It carries the operation name.** Every response remembers which method and resource produced it (e.g. `GET /products`). Assertions can therefore generate descriptive error messages without the test having to repeat the URL by hand.

### `Clients/ApiClientBase.cs`

The base class that every resource client inherits from. It exposes four protected helpers — `GetAsync`, `PostJsonAsync`, `PutJsonAsync`, `DeleteAsync` — each taking a URL path and returning an `ApiResponse<T>`.

It also handles:
- Borrowing the shared `RestClient` from the cache.
- Translating RestSharp's response into our `ApiResponse<T>`.
- Capturing the method and resource so the operation name flows into every response.

This is the most "framework-y" file in Core. It exists so resource clients (`ProductsClient`, `CartsClient`) can stay tiny.

### `Clients/ProductsClient.cs` and `Clients/CartsClient.cs`

Each is a few dozen lines. They expose only what the resource supports — `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, etc. — and each one is a thin one-liner over the base class:

```csharp
public Task<ApiResponse<List<Product>>> GetAllAsync(...) =>
    GetAsync<List<Product>>("/products", ...);
```

A reader can scan a 50-line client file and immediately see every endpoint we've wired up. No magic.

### `Http/SharedRestClient.cs`

A static cache of `RestClient` instances keyed by the configuration. RestSharp's official guidance is to **reuse a single `RestClient`** for the lifetime of an application — creating one per test would leak sockets and waste connections.

In a test framework, "an application" means "the whole test run." The first test that needs a particular base URL triggers a single client to be built and stored; every subsequent test reuses it. At the end of the run, `DisposeAll()` cleans them all up.

### `Http/LoggingHandler.cs`, `TranscriptHandler.cs`, `ResilienceHandler.cs`

These are HTTP **delegating handlers** — small classes that sit in a chain in front of the actual network call. Each one does one thing:

- **`LoggingHandler`** — writes request and response info to the console using Microsoft's standard `ILogger`.
- **`TranscriptHandler`** — records the same info in an in-memory list, scoped to the current test (using `AsyncLocal<T>` so parallel tests don't see each other's data). The transcript gets attached to the failure report when a test fails.
- **`ResilienceHandler`** — uses the Polly library to retry failed requests. Only retries on 5xx and network errors. **Never** retries on 4xx — those are real test failures we want to see.

They're separate because **single responsibility** is easier to reason about. Want logging without transcripts? Set one flag. Want retries without logging? Set another. The handlers are stacked outermost-to-innermost as `Logging → Transcript → Resilience → network`. That order means a single retry produces only one log line and one transcript entry.

### `Http/HttpRedactor.cs`

A small utility that scrubs values like `password=...` and `"token":"..."` from logged URLs and bodies. Both the logger and the transcript route through it, so we can never accidentally log a secret in only one of the two places.

### `Diagnostics/HttpTranscript.cs` and `HttpTranscriptEntry.cs`

The in-memory log of HTTP calls for the current test. It uses `AsyncLocal<T>`, which is C#'s way of saying *"this value is unique to each running test, even if multiple tests run in parallel."* When a test fails, the transcript is formatted and dropped into both the test output and the Extent report.

---

## The Tests project, piece by piece

### `Tests/SmokeTests.cs`, `ProductTests.cs`, `CartTests.cs`, `ProductCartWorkflowTests.cs`

The actual test scenarios. Each fixture is a class with `[TestFixture]`; each test is a method with `[Test]` (or `[TestCaseSource(...)]` for data-driven tests).

`SmokeTests` are fast, reliable checks — they verify the API is reachable. The others are fuller scenarios.

Tests are categorised so we can filter them:
- `[Category("smoke")]` — fast, run on every commit
- `[Category("Product")]`, `[Category("Cart")]` — by feature area
- `[Category("mock-quirk")]` + `[Explicit]` — tests that demonstrate FakeStore mock-API limitations

Run subsets with `dotnet test --filter Category=smoke`.

### `Support/ApiTestBase.cs`

The base class every test fixture inherits from. It provides ready-to-use `Products` and `Carts` clients, created automatically in `[SetUp]`. Test bodies stay focused on the scenario, not on plumbing.

### `Support/ApiResponseAssertions.cs`

Extension methods like `ShouldHaveStatus` and `ShouldHaveData`. These work on any `ApiResponse<T>` thanks to C# generics — write the assertion once, use it for every endpoint.

If you don't pass an operation name, the assertion derives it from the response (`GET /products`, `POST /carts`). The error messages stay informative without the tests having to repeat strings.

### `Support/Scenario.cs`

A two-line class with one method: `Scenario.Step("description")`. It writes the description to the console **and** to the ExtentReports HTML report.

This is the framework's gift to non-developers. Every test has lines like:

```csharp
Scenario.Step("Find the cheapest product in the 'electronics' category");
var cheapest = products.CheapestInCategory("electronics");
```

A non-developer can read just the step descriptions and follow the test, even without reading any C# at all. The HTML report becomes a living specification.

### `Support/ProductQueries.cs` and `Support/CartQueries.cs`

Extension methods that hide LINQ behind named queries:

```csharp
products.CheapestInCategory("electronics")   // instead of .Where(...).OrderBy(...).FirstOrDefault()
products.LowestRated()
carts.HighestId()
```

Tests express **intent**; the helpers handle the syntax. This is the second readability tool, working alongside `Scenario.Step`.

### `TestData/CartBuilder.cs`

A factory that produces `CreateCartRequest` objects with one line:

```csharp
var request = CartBuilder.WithProduct(productId);
```

Without it, every test that creates a cart had a 12-line object initialiser. The builder hides date formatting, the default user ID, and the list-of-one nesting.

### `TestData/ProductTestData.cs`

A static source of test data for parameterised tests. NUnit picks it up via `[TestCaseSource(...)]` and runs the test once per row. Useful when the same test body needs to be exercised against multiple inputs.

### `Reporting/`

Three pieces, each with one job:

- **`ExtentReportManager`** — owns the ExtentReports object and writes the HTML report to `TestResults/ExtentReports/{timestamp}/index.html`.
- **`ExtentReportRunHooks`** — an NUnit `[SetUpFixture]` (in the global namespace, so the test explorer doesn't nest every test under it) that initialises the report at the start of the run and flushes it at the end. It also disposes the shared `RestClient`s.
- **`ExtentTestContext`** — connects each running test to its node in the report and logs `Scenario.Step` calls into the report tree.

### `Diagnostics/HttpTranscriptActionAttribute.cs` and `Reporting/ExtentReportingActionAttribute.cs`

NUnit `TestActionAttribute`s registered at the assembly level. They run automatically before and after every test:

- **`HttpTranscriptAction`** — clears the transcript before each test (so tests don't see each other's data) and writes it to the failure log if the test failed.
- **`ExtentReportingAction`** — creates a node in the Extent report for the current test, then closes it with the result (passed/failed/skipped).

Two small attributes is easier to understand than one attribute that does both.

### `AssemblyAttributes.cs`

A single file declaring both `[assembly:]` attributes in a deterministic order, with a comment explaining why the order matters. C# doesn't guarantee an order across files, so consolidating them ensures the test-action callbacks always fire in the right sequence.

---

## Key design decisions, with the reason for each

**Two projects, not one.** The Core library can be reused by other test suites. The tests aren't coupled to the framework's internals.

**Custom `ApiResponse<T>`, not RestSharp's `RestResponse<T>`.** No test ever has `using RestSharp;` at the top. If we change HTTP libraries, only `ApiClientBase` changes.

**One client class per resource.** `ProductsClient.GetAllAsync()` is shorter, more discoverable, and groups related operations together better than the alternatives.

**Configuration via `appsettings.json` plus environment variables.** A single source of truth that supports both local development and CI overrides without code changes.

**Shared `RestClient`.** RestSharp's documented best practice. Avoids socket exhaustion under load and amortises the cost of HTTP/2 connection reuse.

**Auto-derived operation names on `ApiResponse<T>`.** Tests don't need to write `"GET /products"` everywhere — the response already knows what produced it. Removes a class of bugs where the string drifts from the actual call.

**Three small handlers, not one big one.** Logging, transcript-recording, and retry are independent capabilities. Each can be turned on or off independently. Easier to reason about and easier to extend.

**`Scenario.Step()` DSL.** The single biggest readability win. A non-developer can read the step descriptions and follow the test without reading any C#. The Extent report becomes a living specification.

**Test data builders and query helpers.** Hide LINQ chains and object initialisers behind named methods. Tests express *what*; helpers express *how*.

**`[Category]` for filtering.** Lets the team run subsets: `smoke` for quick feedback, `mock-quirk` to demonstrate FakeStore's limitations on demand.

**`[Explicit]` for known-failing demonstration tests.** Tests that prove a mock-API limitation are explicitly opted-in. The default `dotnet test` run is green.

**ExtentReports for HTML output.** Generates the report directly during the test run — no separate CLI step, no Java dependency. The library is sunset, which is a known tradeoff; switching to Allure would be a self-contained change if needed.

**Polly for retries.** Industry standard. Retries only on transient failures (5xx, network errors). Never on 4xx, because hiding test failures is worse than letting them surface.

---

## What's deliberately NOT in the framework

For an interview, knowing what to leave out is as important as what to include:

- **No dependency injection container.** The framework is small enough that manual wiring is clear and direct. Adding a container before there's pain would obscure the design.
- **No generic `ResourceClient<T>` base.** Only two clients exist; deduplicating them would save ten lines and add an abstraction. Wait until there's a third or fourth client to introduce it.
- **No mocking layer for the API.** The point is to test against the real (mock) FakeStore endpoints. Mocking the mock would defeat the purpose.
- **No fluent assertion library (FluentAssertions, Shouldly).** NUnit's built-in `Assert.That(...)` is enough for a framework this size.
- **No BDD layer (Reqnroll, SpecFlow).** The `Scenario.Step()` DSL gives 80% of the readability benefit at 5% of the complexity cost.

The framework is small on purpose. Every line of "framework" is a line that test authors have to understand and that maintainers have to keep working. Less is more, until it isn't.

---

## How to demo it

```bash
# Default run — green build, smoke tests only
dotnet test

# Smoke tests, named explicitly
dotnet test --filter Category=smoke

# Mock-API quirk demonstrations (these intentionally fail)
dotnet test --filter Category=mock-quirk

# All product-related tests
dotnet test --filter Category=Product
```

After any run, the HTML report appears in `TestResults/ExtentReports/{timestamp}/index.html`. Open it in a browser to see passing/failing tests, narrated steps, and the captured HTTP transcript for any failures.
