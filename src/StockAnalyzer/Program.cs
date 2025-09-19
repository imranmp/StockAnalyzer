using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using StockAnalyzer.Services;
using StockAnalyzer.Services.Interfaces;
using System.Net;

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

// Logging (important for container stdout/stderr)
services.AddLogging(logging => logging.AddConsole());

// Named HttpClient that uses the above pipeline
services.AddHttpClient("Finnhub", client =>
{
    client.BaseAddress = new Uri("https://finnhub.io/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddStandardResilienceHandler()
    .Configure((HttpStandardResilienceOptions options, IServiceProvider services) =>
    {
        // 1) total retry attempts (in addition to the original call)
        options.Retry.MaxRetryAttempts = 5;

        // 2) custom delay: 5 seconds * attempt number (attemptNumber is zero-based,
        //    but the first retry attempt will have AttemptNumber == 1)
        options.Retry.DelayGenerator = args =>
            ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(5 * Math.Max(1, args.AttemptNumber)));

        // 3) only handle 429 and 5xx status codes (and non-cancellation exceptions)
        options.Retry.ShouldHandle = args =>
        {
            var outcome = args.Outcome;

            if (outcome.Result is HttpResponseMessage resp)
            {
                var code = resp.StatusCode;
                return ValueTask.FromResult(code == HttpStatusCode.TooManyRequests || (int)code >= 500);
            }

            if (outcome.Exception is not null)
            {
                // retry for exceptions except cancellation
                return ValueTask.FromResult(!(outcome.Exception is OperationCanceledException));
            }

            return ValueTask.FromResult(false);
        };

        // 4) get an ILogger from the IServiceProvider and log on retry
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


// Register application services
services.AddSingleton<ITickerProvider, TickerProvider>();
services.AddSingleton<IAnalysisFetcher, AnalysisFetcher>();
services.AddSingleton<IFileStorage, FileStorage>();
services.AddTransient<App>();

var serviceProvider = services.BuildServiceProvider();

var app = serviceProvider.GetRequiredService<App>();
await app.RunAsync();