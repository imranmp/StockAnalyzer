using StockAnalyzer.Services.Interfaces;

namespace StockAnalyzer.Services;

public class TickerProvider(string? tickersFile = null) : ITickerProvider
{
    // ticker file name/path can be provided during construction (e.g. from args or DI)
    private const string DefaultTickersFile = "Tickers.txt";
    private readonly string _tickersFile = string.IsNullOrWhiteSpace(tickersFile) ? DefaultTickersFile : tickersFile;

    public async Task<List<(string ticker, string companyName)>> GetTickersAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_tickersFile))
        {
            return [];
        }

        const int maxLines = 200;

        var lines = await File.ReadAllLinesAsync(_tickersFile, cancellationToken);
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        if (nonEmptyLines.Count > maxLines)
        {
            throw new InvalidOperationException($"Tickers file '{_tickersFile}' contains {nonEmptyLines.Count} entries which exceeds the maximum allowed of {maxLines}.");
        }

        var tickers = new List<(string ticker, string companyName)>();

        foreach (var line in nonEmptyLines)
        {
            var trimmed = line.Trim();

            // Split on first tab or comma only
            var parts = trimmed.Split(['\t', ','], 2);
            var ticker = parts[0].Trim();
            if (string.IsNullOrEmpty(ticker))
            {
                continue;
            }

            var company = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            tickers.Add((ticker, company));
        }

        return tickers
            .DistinctBy(x => x.ticker)
            .OrderBy(x => x.ticker)
            .ToList();
    }
}
