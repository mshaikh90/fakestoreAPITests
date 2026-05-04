using Polly;
using Polly.Retry;

namespace FakeStoreApi.Core.Http;

/// <summary>
/// Retries transient HTTP failures (5xx responses, network errors) using Polly.
/// Sits as the innermost handler in the chain so retries are transparent to logging
/// and transcript handlers — outer handlers see one logical request regardless of attempts.
/// 4xx responses are NEVER retried — those represent real test failures.
/// </summary>
internal sealed class ResilienceHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public ResilienceHandler(int maxRetryAttempts, TimeSpan baseDelay)
    {
        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => (int)response.StatusCode >= 500)
                    .Handle<HttpRequestException>(),
                Delay = baseDelay,
                MaxRetryAttempts = maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _pipeline
            .ExecuteAsync(async ct => await base.SendAsync(request, ct), cancellationToken)
            .ConfigureAwait(false);
    }
}
