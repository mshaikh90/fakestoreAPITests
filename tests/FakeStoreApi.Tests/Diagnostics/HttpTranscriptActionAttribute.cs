using FakeStoreApi.Core.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace FakeStoreApi.Tests.Diagnostics;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class HttpTranscriptActionAttribute : TestActionAttribute
{
    public override ActionTargets Targets => ActionTargets.Test;

    public override void BeforeTest(ITest test)
    {
        HttpTranscript.Clear();
    }

    public override void AfterTest(ITest test)
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            var transcript = HttpTranscript.FormatForFailure(test.FullName);
            TestContext.Error.WriteLine(transcript);
        }

        HttpTranscript.Clear();
    }
}
