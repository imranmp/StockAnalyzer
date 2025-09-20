namespace StockAnalyzer.Services.Interfaces;

public interface ITickerProvider
{
    Task<List<(string ticker, string companyName)>> GetTickersAsync(CancellationToken cancellationToken = default);
}
