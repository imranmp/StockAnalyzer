using Microsoft.Extensions.Logging;
using StockAnalyzer.Services.Interfaces;

namespace StockAnalyzer.Services;

public class App
{
    private readonly ITickerProvider _tickerProvider;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<App> _logger;

    public App(ITickerProvider tickerProvider,
               IFileStorage fileStorage,
               ILogger<App> logger)
    {
        _tickerProvider = tickerProvider;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        //Load tickers from Tickers.txt
        List<(string ticker, string companyName)> tickers = await _tickerProvider.GetTickersAsync();
        if (tickers.Count == 0)
        {
            _logger.LogWarning("No tickers found in Tickers.txt");
            return;
        }

        //Load existing analysis from JSON file
        var analyses = await _fileStorage.LoadAnalysisAsync();

        //Update and save analysis to JSON file
        analyses = await _fileStorage.SaveAnalysisAsync(tickers, analyses);

        //Export analysis to CSV file
        _fileStorage.OpenWriteStream("AnalysisResult.csv", analyses);

        _logger.LogInformation("Analysis completed successfully!");
    }
}
