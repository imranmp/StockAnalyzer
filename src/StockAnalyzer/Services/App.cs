using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockAnalyzer.Services.Interfaces;

namespace StockAnalyzer.Services;

public class App
{
    private readonly ITickerProvider _tickerProvider;
    private readonly IAnalysisFetcher _analysisFetcher;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<App> _logger;

    public App(ITickerProvider tickerProvider,
               IAnalysisFetcher analysisFetcher,
               IFileStorage fileStorage,
               IConfiguration configuration,
               ILogger<App> logger)
    {
        _tickerProvider = tickerProvider;
        _analysisFetcher = analysisFetcher;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        string? finnhubToken = _configuration["FinnhubToken"];
        if (string.IsNullOrWhiteSpace(finnhubToken))
        {
            _logger.LogError("FinnhubToken is not configured. Set the FinnhubToken in appsettings or user secrets.");
            return;
        }

        List<(string ticker, string companyName)> tickers = await _tickerProvider.GetTickersAsync();
        if (tickers.Count == 0)
        {
            _logger.LogWarning("No tickers found in Tickers.txt");
            return;
        }

        var analyses = await _fileStorage.LoadAnalysisAsync();

        foreach (var (ticker, companyName) in tickers)
        {
            _logger.LogInformation("Processing {Ticker}", ticker);
            var results = await _analysisFetcher.FetchAnalysisAsync(ticker, finnhubToken!);
            if (results != null && results.Count != 0)
            {
                foreach (var entry in results)
                {
                    var existing = analyses.FirstOrDefault(a => a.Symbol == entry.Symbol && a.Period == entry.Period);
                    if (existing != null)
                    {
                        // replace
                        analyses.Remove(existing);
                        analyses.Add(entry);
                    }
                    else
                    {
                        analyses.Add(entry);
                    }
                }
            }
        }

        analyses = [.. analyses.OrderBy(a => a.Symbol).ThenByDescending(a => a.Period)];

        await _fileStorage.SaveAnalysisAsync(analyses);

        using var csvStream = _fileStorage.OpenWriteStream("AnalysisResult.csv");
        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        using var writer = new StreamWriter(csvStream);
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(analyses);

        _logger.LogInformation("Analysis completed successfully!");
    }
}
