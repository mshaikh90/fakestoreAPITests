using FakeStoreApi.Core.Http;
using FakeStoreApi.Tests.Reporting;

// Intentionally global (no namespace) so the SetUpFixture applies to the entire assembly
// without nesting every test under it in the test explorer's namespace tree.
[SetUpFixture]
internal sealed class ExtentReportRunHooks
{
    [OneTimeSetUp]
    public void BeforeTestRun()
    {
        ExtentReportManager.EnsureInitialized();
        TestContext.Progress.WriteLine($"Extent report: {ExtentReportManager.ReportPath}");
    }

    [OneTimeTearDown]
    public void AfterTestRun()
    {
        ExtentReportManager.Flush();
        TestContext.Progress.WriteLine($"Extent report generated: {ExtentReportManager.ReportPath}");

        SharedRestClient.DisposeAll();
    }
}
