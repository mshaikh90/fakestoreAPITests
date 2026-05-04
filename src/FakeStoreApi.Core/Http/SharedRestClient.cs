using System.Collections.Concurrent;
using System.Text.Json;
using FakeStoreApi.Core.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Serializers.Json;

namespace FakeStoreApi.Core.Http;

/// <summary>
/// Process-wide cache of <see cref="RestClient"/> instances keyed by <see cref="ApiClientOptions"/>.
/// RestSharp recommends reusing a single client; this cache enforces that without making
/// callers manage lifetime. Disposed once at the end of the test run via <see cref="DisposeAll"/>.
/// </summary>
public static class SharedRestClient
{
    private static readonly ConcurrentDictionary<string, Entry> Cache = new();

    public static RestClient GetOrCreate(ApiClientOptions options, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var key = ComputeKey(options);
        return Cache.GetOrAdd(key, _ => Build(options, loggerFactory)).Client;
    }

    public static void DisposeAll()
    {
        foreach (var entry in Cache.Values)
        {
            entry.Client.Dispose();
            entry.OwnedLoggerFactory?.Dispose();
        }

        Cache.Clear();
    }

    private static string ComputeKey(ApiClientOptions options) =>
        string.Join("|",
            options.BaseUrl,
            options.Timeout,
            options.EnableHttpLogging,
            options.CaptureHttpTranscript,
            options.LogRequestBody,
            options.LogResponseBody,
            options.MaxLoggedBodyLength,
            options.EnableRetry,
            options.MaxRetryAttempts,
            options.RetryBaseDelay);

    private static Entry Build(ApiClientOptions options, ILoggerFactory? loggerFactory)
    {
        ILoggerFactory? ownedLoggerFactory = null;
        var activeLoggerFactory = loggerFactory;

        if (options.EnableHttpLogging && activeLoggerFactory is null)
        {
            ownedLoggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(console =>
            {
                console.SingleLine = true;
                console.TimestampFormat = "HH:mm:ss ";
            }));
            activeLoggerFactory = ownedLoggerFactory;
        }

        var clientOptions = new RestClientOptions(options.BaseUrl)
        {
            Timeout = options.Timeout,
            ConfigureMessageHandler = inner => BuildHandlerChain(inner, options, activeLoggerFactory)
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var client = new RestClient(
            clientOptions,
            configureSerialization: serializer => serializer.UseSystemTextJson(jsonOptions));

        return new Entry(client, ownedLoggerFactory);
    }

    private static HttpMessageHandler BuildHandlerChain(
        HttpMessageHandler innerHandler,
        ApiClientOptions options,
        ILoggerFactory? loggerFactory)
    {
        var chain = innerHandler;

        // Resilience is innermost: retries are transparent to logging and transcript handlers,
        // so outer handlers see one logical request even if the network call is retried.
        if (options.EnableRetry)
            chain = new ResilienceHandler(options.MaxRetryAttempts, options.RetryBaseDelay) { InnerHandler = chain };

        if (options.CaptureHttpTranscript)
            chain = new TranscriptHandler { InnerHandler = chain };

        if (options.EnableHttpLogging && loggerFactory is not null)
        {
            var logger = loggerFactory.CreateLogger("HTTP");
            chain = new LoggingHandler(logger, options) { InnerHandler = chain };
        }

        return chain;
    }

    private sealed record Entry(RestClient Client, ILoggerFactory? OwnedLoggerFactory);
}
