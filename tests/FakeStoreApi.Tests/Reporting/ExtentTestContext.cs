using AventStack.ExtentReports;
using AventStack.ExtentReports.MarkupUtils;
using NUnit.Framework.Interfaces;

namespace FakeStoreApi.Tests.Reporting;

public static class ExtentTestContext
{
    private static readonly AsyncLocal<ExtentTest?> CurrentTest = new();

    public static void StartTest(ITest test)
    {
        ArgumentNullException.ThrowIfNull(test);

        var displayName = string.IsNullOrWhiteSpace(test.MethodName)
            ? test.Name
            : test.MethodName;

        CurrentTest.Value = ExtentReportManager.Instance.CreateTest(displayName)
            .AssignCategory(test.ClassName ?? "Uncategorized");
    }

    public static void LogStep(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        CurrentTest.Value?.Info(description);
    }

    public static void FinishTest(TestContext context, string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);

        var test = CurrentTest.Value;
        if (test is null)
            return;

        var result = context.Result;
        var status = result.Outcome.Status;

        if (status == TestStatus.Passed)
        {
            test.Pass("Test passed.");
        }
        else if (status == TestStatus.Skipped)
        {
            test.Skip(result.Message ?? "Test skipped.");
        }
        else if (status == TestStatus.Failed)
        {
            test.Fail(result.Message ?? "Test failed.");

            if (!string.IsNullOrWhiteSpace(result.StackTrace))
                test.Info(MarkupHelper.CreateCodeBlock(result.StackTrace));

            if (!string.IsNullOrWhiteSpace(transcript))
                test.Info(MarkupHelper.CreateCodeBlock(transcript));
        }
        else
        {
            test.Warning(result.Message ?? $"Test finished with status: {status}");
        }

        CurrentTest.Value = null;
    }
}
