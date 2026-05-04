using FakeStoreApi.Tests.Reporting;

namespace FakeStoreApi.Tests.Support;

/// <summary>
/// Step narration for tests. Each call describes the next step in plain English,
/// writing to the console and the Extent report so that anyone reading either
/// can follow the scenario without reading the C# code.
/// </summary>
public static class Scenario
{
    public static void Step(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        TestContext.Out.WriteLine($"  → {description}");
        ExtentTestContext.LogStep(description);
    }
}
