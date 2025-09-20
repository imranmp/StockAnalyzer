using StockAnalyzer.Services.Interfaces;

namespace StockAnalyzer.Services;

public class TickerProvider : ITickerProvider
{
    //TODO: ticker file name/path should come as an argument during execution
    private const string TickersFile = "Tickers.txt";

    public async Task<List<(string ticker, string companyName)>> GetTickersAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(TickersFile))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(TickersFile);
        var tickers = new List<(string ticker, string companyName)>();

        //TODO: limit lines to 200 to avoid too many requests in a short time
        //TODO: allow lines to be comma or tab separated
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split('\t');
            if (parts.Length >= 2)
            {
                tickers.Add((parts[0], parts[1]));
            }
            else if (parts.Length == 1)
            {
                tickers.Add((parts[0], string.Empty));
            }
        }

        return [.. tickers.DistinctBy(x => x.ticker).OrderBy(x => x.ticker)];
    }
}
