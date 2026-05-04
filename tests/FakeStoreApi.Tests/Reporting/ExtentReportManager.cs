using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Config;

namespace FakeStoreApi.Tests.Reporting;

public static class ExtentReportManager
{
    private static readonly object SyncRoot = new();
    private static ExtentReports? _extent;

    public static string ReportPath { get; private set; } = string.Empty;

    public static ExtentReports Instance
    {
        get
        {
            EnsureInitialized();
            return _extent!;
        }
    }

    public static void EnsureInitialized()
    {
        if (_extent is not null)
            return;

        lock (SyncRoot)
        {
            if (_extent is not null)
                return;

            var runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var reportDirectory = Path.Combine(FindSolutionRoot(), "TestResults", "ExtentReports", runId);

            Directory.CreateDirectory(reportDirectory);

            ReportPath = Path.Combine(reportDirectory, "index.html");

            var reporter = new ExtentSparkReporter(ReportPath);
            reporter.Config.DocumentTitle = "FakeStore API Automation Report";
            reporter.Config.ReportName = "FakeStore API Test Results";
            reporter.Config.Theme = Theme.Standard;

            var extent = new ExtentReports();
            extent.AttachReporter(reporter);
            extent.AddSystemInfo("Framework", ".NET " + Environment.Version);
            extent.AddSystemInfo("Machine", Environment.MachineName);
            extent.AddSystemInfo("OS", Environment.OSVersion.ToString());
            extent.AddSystemInfo("Environment", Environment.GetEnvironmentVariable("TEST_ENV") ?? "development");

            _extent = extent;
        }
    }

    public static void Flush()
    {
        lock (SyncRoot)
        {
            _extent?.Flush();
        }
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("*.slnx").Length > 0)
                return directory.FullName;

            directory = directory.Parent;
        }

        return TestContext.CurrentContext.WorkDirectory;
    }
}
