using System.Net;
using FakeStoreApi.Core.Http;

namespace FakeStoreApi.Tests.Support;

public static class ApiResponseAssertions
{
    public static void ShouldHaveStatus<T>(
        this ApiResponse<T> response,
        HttpStatusCode expectedStatus,
        string? operation = null)
    {
        operation ??= response.Operation;

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus),
            $"{operation} returned unexpected status. Body: {response.RawContent}");
    }

    public static T ShouldHaveData<T>(
        this ApiResponse<T> response,
        HttpStatusCode expectedStatus,
        string? operation = null)
    {
        operation ??= response.Operation;

        response.ShouldHaveStatus(expectedStatus, operation);
        Assert.That(response.Data, Is.Not.Null, $"{operation} did not return a response body.");

        return response.Data!;
    }
}
