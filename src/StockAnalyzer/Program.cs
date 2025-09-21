using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using StockAnalyzer.Services;
using StockAnalyzer.Services.Interfaces;

// Build configuration
var configuration = BuildConfiguration();

// Configure DI and logging early so we can log validation issues
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
ConfigureLogging(services, configuration);

// Temporary provider to get a logger for early validation messages
using var tempProvider = services.BuildServiceProvider();
var logger = tempProvider.GetRequiredService<ILogger<Program>>();

// Validate required configuration
var finnhubToken = configuration["FinnhubToken"];
if (string.IsNullOrWhiteSpace(finnhubToken))
{
    logger.LogError("FinnhubToken is not configured. Set the FinnhubToken in appsettings.");
    return;
}

// Configure named HttpClient (uses resilience handler)
ConfigureHttpClient(services, finnhubToken);

// Resolve files and register services (extracts registration into helper)
var (tickersFile, analysisFile) = ResolveFilePaths(configuration, logger, args);
RegisterServices(services, tickersFile, analysisFile);

using var serviceProvider = services.BuildServiceProvider();
var app = serviceProvider.GetRequiredService<App>();
await app.RunAsync();


static IConfiguration BuildConfiguration()
{
    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true);

    return builder.Build();
}

static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
{
    services.AddLogging(logging =>
    {
        logging.AddConfiguration(configuration.GetSection("Logging"));
        logging.AddConsole();
    });
}

static void ConfigureHttpClient(IServiceCollection services, string finnhubToken)
{
    services.AddHttpClient("Finnhub", client =>
    {
        client.BaseAddress = new Uri("https://finnhub.io/");
        client.DefaultRequestHeaders.Remove("X-Finnhub-Token");
        client.DefaultRequestHeaders.Add("X-Finnhub-Token", finnhubToken);

        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler()
    .Configure((HttpStandardResilienceOptions options, IServiceProvider sp) =>
    {
        options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(120)
        };

        options.Retry.MaxRetryAttempts = 5;
        options.Retry.Delay = TimeSpan.FromSeconds(4);
        options.Retry.UseJitter = true;
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HttpRetry");
        options.Retry.OnRetry = args =>
        {
            // AttemptNumber is zero-based (first retry is 1)
            logger.LogWarning(
                "Retry # {RetryCount} for request {RequestUri} due to {Reason}",
                args.AttemptNumber,
                args.Outcome.Result?.RequestMessage?.RequestUri,
                args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name
            );

            return ValueTask.CompletedTask;
        };
    });
}

static (string tickersFile, string analysisFile) ResolveFilePaths(IConfiguration configuration, ILogger logger, string[] args)
{
    var tickersFile = GetArgValue("tickers-file", args) ?? configuration["TickersFile"] ?? "Tickers.txt";
    var analysisFile = GetArgValue("analysis-file", args) ?? configuration["AnalysisFile"] ?? "AnalysisResult.json";

    logger.LogInformation("Using tickers file: {TickersFile}", tickersFile);
    logger.LogInformation("Using analysis file: {AnalysisFile}", analysisFile);

    return (tickersFile, analysisFile);
}

static void RegisterServices(IServiceCollection services, string tickersFile, string analysisFile)
{
    // Register file- and domain-related services in one place for clarity
    services.AddSingleton<ITickerProvider>(sp => new TickerProvider(tickersFile));

    services.AddSingleton<IAnalysisFetcher, AnalysisFetcher>();

    services.AddSingleton<IFileStorage>(sp => new FileStorage(sp.GetRequiredService<ILogger<FileStorage>>(),
        sp.GetRequiredService<IAnalysisFetcher>(), analysisFile));

    services.AddTransient<App>();
}

static string? GetArgValue(string key, string[] args)
{
    if (args is null || args.Length == 0)
    {
        return null;
    }

    var prefix = "--" + key;
    for (int i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], prefix, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1];
        }

        if (args[i].StartsWith(prefix + "=", StringComparison.OrdinalIgnoreCase))
        {
            return args[i][(prefix.Length + 1)..];
        }
    }

    return null;
}