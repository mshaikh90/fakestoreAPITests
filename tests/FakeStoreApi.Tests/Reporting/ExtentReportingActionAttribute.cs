using FakeStoreApi.Core.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace FakeStoreApi.Tests.Reporting;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ExtentReportingActionAttribute : TestActionAttribute
{
    public override ActionTargets Targets => ActionTargets.Test;

    public override void BeforeTest(ITest test)
    {
        ExtentTestContext.StartTest(test);
    }

    public override void AfterTest(ITest test)
    {
        var transcript = HttpTranscript.FormatForFailure(test.FullName);
        ExtentTestContext.FinishTest(TestContext.CurrentContext, transcript);
    }
}
