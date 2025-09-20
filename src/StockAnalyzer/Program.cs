using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using StockAnalyzer.Services;
using StockAnalyzer.Services.Interfaces;

// Setup configuration to read from appsettings, user secrets and environment variables
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

var configuration = builder.Build();

// Setup dependency injection
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

// Configure logging to read configuration and add console provider
services.AddLogging(logging =>
{
    logging.AddConfiguration(configuration.GetSection("Logging"));
    logging.AddConsole();
});

var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

// Named HttpClient that uses the above pipeline
string? finnhubToken = configuration["FinnhubToken"];
if (string.IsNullOrWhiteSpace(finnhubToken))
{
    logger.LogError("FinnhubToken is not configured. Set the FinnhubToken in appsettings.");
    return;
}

services.AddHttpClient("Finnhub", client =>
{
    client.BaseAddress = new Uri("https://finnhub.io/");

    client.DefaultRequestHeaders.Remove("X-Finnhub-Token");
    client.DefaultRequestHeaders.Add("X-Finnhub-Token", finnhubToken);

    //client.Timeout = TimeSpan.FromSeconds(30);

}).AddStandardResilienceHandler()
    .Configure((HttpStandardResilienceOptions options, IServiceProvider services) =>
    {
        //options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
        //{
        //    Timeout = TimeSpan.FromSeconds(30)
        //};

        //options.AttemptTimeout = new HttpTimeoutStrategyOptions
        //{
        //    Timeout = TimeSpan.FromSeconds(10)
        //};

        options.Retry.MaxRetryAttempts = 4;
        options.Retry.Delay = TimeSpan.FromSeconds(5);

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("HttpRetry");
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

services.AddSingleton<ITickerProvider, TickerProvider>();
services.AddSingleton<IAnalysisFetcher, AnalysisFetcher>();
services.AddSingleton<IFileStorage, FileStorage>();
services.AddTransient<App>();

var serviceProvider = services.BuildServiceProvider();

var app = serviceProvider.GetRequiredService<App>();
await app.RunAsync();