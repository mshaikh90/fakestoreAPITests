using FakeStoreApi.Tests.Diagnostics;
using FakeStoreApi.Tests.Reporting;
using NUnit.Framework;

// Declaration order matters. NUnit runs TestActions in declared order for BeforeTest
// and reverse order for AfterTest. With this ordering:
//   BeforeTest:  HttpTranscript (clear) -> Extent (start test)
//   AfterTest:   Extent (read transcript, finish test) -> HttpTranscript (clear)
// This ensures Extent can read the transcript before HttpTranscript clears it.
[assembly: HttpTranscriptAction]
[assembly: ExtentReportingAction]

// Run test fixtures in parallel with each other. Tests within a fixture still run
// sequentially, which keeps shared per-fixture state safe. The framework's HTTP
// transcript uses AsyncLocal<T> and the ExtentReports/RestClient singletons are
// thread-safe, so cross-fixture parallelism is supported by design.
[assembly: Parallelizable(ParallelScope.Fixtures)]
