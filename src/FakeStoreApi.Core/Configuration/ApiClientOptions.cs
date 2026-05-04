using System.Text.Json;

namespace FakeStoreApi.Core.Configuration;

public sealed record ApiClientOptions
{
    public Uri BaseUrl { get; init; } = new("https://fakestoreapi.com/");

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public bool EnableHttpLogging { get; init; } = true;

    public bool CaptureHttpTranscript { get; init; } = true;

    public bool LogRequestBody { get; init; }

    public bool LogResponseBody { get; init; }

    public int MaxLoggedBodyLength { get; init; } = 4_000;

    public bool EnableRetry { get; init; } = true;

    public int MaxRetryAttempts { get; init; } = 2;

    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    public static ApiClientOptions FromConfiguration()
    {
        var defaults = new ApiClientOptions();
        var configured = LoadAppSettings().ApiClient;

        return defaults with
        {
            BaseUrl = GetUri("API_BASE_URL", configured.BaseUrl, defaults.BaseUrl),
            Timeout = TimeSpan.FromSeconds(GetPositiveInteger(
                "API_TIMEOUT_SECONDS",
                configured.TimeoutSeconds,
                (int)defaults.Timeout.TotalSeconds)),
            EnableHttpLogging = GetBoolean(
                "API_ENABLE_HTTP_LOGGING",
                configured.EnableHttpLogging,
                defaults.EnableHttpLogging),
            CaptureHttpTranscript = GetBoolean(
                "API_CAPTURE_HTTP_TRANSCRIPT",
                configured.CaptureHttpTranscript,
                defaults.CaptureHttpTranscript),
            LogRequestBody = GetBoolean(
                "API_LOG_REQUEST_BODY",
                configured.LogRequestBody,
                defaults.LogRequestBody),
            LogResponseBody = GetBoolean(
                "API_LOG_RESPONSE_BODY",
                configured.LogResponseBody,
                defaults.LogResponseBody),
            MaxLoggedBodyLength = GetPositiveInteger(
                "API_MAX_LOGGED_BODY_LENGTH",
                configured.MaxLoggedBodyLength,
                defaults.MaxLoggedBodyLength),
            EnableRetry = GetBoolean(
                "API_ENABLE_RETRY",
                configured.EnableRetry,
                defaults.EnableRetry),
            MaxRetryAttempts = GetPositiveInteger(
                "API_MAX_RETRY_ATTEMPTS",
                configured.MaxRetryAttempts,
                defaults.MaxRetryAttempts),
            RetryBaseDelay = TimeSpan.FromMilliseconds(GetPositiveInteger(
                "API_RETRY_BASE_DELAY_MS",
                configured.RetryBaseDelayMilliseconds,
                (int)defaults.RetryBaseDelay.TotalMilliseconds))
        };
    }

    private static AppSettings LoadAppSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Unable to parse configuration file '{path}'.", exception);
        }
    }

    private static Uri GetUri(string name, string? configuredValue, Uri defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (Uri.TryCreate(raw, UriKind.Absolute, out var environmentValue))
            return environmentValue;

        return Uri.TryCreate(configuredValue, UriKind.Absolute, out var configured)
            ? configured
            : defaultValue;
    }

    private static int GetPositiveInteger(string name, int? configuredValue, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (int.TryParse(raw, out var parsed) && parsed > 0)
            return parsed;

        return configuredValue is > 0 ? configuredValue.Value : defaultValue;
    }

    private static bool GetBoolean(string name, bool? configuredValue, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(raw, out var parsed)
            ? parsed
            : configuredValue ?? defaultValue;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private sealed record AppSettings
    {
        public ApiClientSettings ApiClient { get; init; } = new();
    }

    private sealed record ApiClientSettings
    {
        public string? BaseUrl { get; init; }

        public int? TimeoutSeconds { get; init; }

        public bool? EnableHttpLogging { get; init; }

        public bool? CaptureHttpTranscript { get; init; }

        public bool? LogRequestBody { get; init; }

        public bool? LogResponseBody { get; init; }

        public int? MaxLoggedBodyLength { get; init; }

        public bool? EnableRetry { get; init; }

        public int? MaxRetryAttempts { get; init; }

        public int? RetryBaseDelayMilliseconds { get; init; }
    }
}
