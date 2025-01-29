using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using StockAnalyzer.Converters;
using StockAnalyzer.Models;
using System.Globalization;
using System.Text.Json;

// Setup configuration to read from user secrets
var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>();

var configuration = builder.Build();

// Get your Finnhub API token from user secrets
string finnhubToken = configuration["FinnhubToken"];

ArgumentNullException.ThrowIfNullOrWhiteSpace(finnhubToken);

// Setup dependency injection
var serviceCollection = new ServiceCollection();
ConfigureServices(serviceCollection);
var serviceProvider = serviceCollection.BuildServiceProvider();

// Get the HttpClientFactory
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

// Your list of tickers
List<(string ticker, string companyName)> tickers = GetTickers();
if (tickers.Count == 0)
{
    Console.WriteLine("No tickers found in Tickers.txt");
    return;
}

List<Analysis> analysis = [];

JsonSerializerOptions options = new()
{
    Converters = { new DecimalConverter(), new DoubleConverter() },
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};

if (File.Exists("AnalysisResult.json"))
{
    string json = File.ReadAllText("AnalysisResult.json");
    if (json.Length != 0)
    {
        analysis = JsonSerializer.Deserialize<List<Analysis>>(json, options)!;
    }
}

foreach ((string ticker, string companyName) in tickers)
{
    string url = $"https://finnhub.io/api/v1/stock/recommendation?symbol={ticker}";
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Finnhub-Token", finnhubToken);

    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == (System.Net.HttpStatusCode)429)
        .WaitAndRetryAsync(5, retryAttempt =>
        {
            Console.WriteLine($"Retrying... Attempt {retryAttempt}");
            return TimeSpan.FromSeconds(5 * retryAttempt);
        });

    HttpResponseMessage response = await retryPolicy.ExecuteAsync(() => client.GetAsync(url));

    Console.Write($"{ticker} ");

    if (response.IsSuccessStatusCode)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        Analysis[]? analyses = JsonSerializer.Deserialize<Analysis[]>(responseBody, options);

        foreach (Analysis a in analyses!)
        {
            if (analysis.Any(c => c.Symbol == a.Symbol && c.Period == a.Period))
            {
                Analysis existingAnalysis = analysis.First(c => c.Symbol == a.Symbol && c.Period == a.Period);
                existingAnalysis = a;
            }
            else
            {
                analysis.Add(a);
            }
        }
    }
    else
    {
        Console.WriteLine("Error");
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}

string analysesResultJson = JsonSerializer.Serialize(analysis, options);
File.WriteAllText("AnalysisResult.json", analysesResultJson);

var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
};

using var writer = new StreamWriter("AnalysisResult.csv");
using var csv = new CsvWriter(writer, config);
csv.WriteRecords(analysis);

Console.WriteLine();
Console.WriteLine("Analysis completed successfully!");

static void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient();
}

static List<(string ticker, string companyName)> GetTickers()
{
    string[] lines = File.ReadAllLines("../../../Tickers.txt");
    List<(string ticker, string companyName)> tickers = [];
    foreach (string line in lines)
    {
        string[] parts = line.Split('\t');
        tickers.Add((parts[0], parts[1]));
    }
    return [.. tickers.DistinctBy(x => x.ticker).OrderBy(x => x.ticker)];
}
