using StockAnalyzer.Models;

namespace StockAnalyzer.Services.Interfaces;

public interface IAnalysisFetcher
{
    Task<List<Analysis>?> FetchAnalysisAsync(string ticker, CancellationToken cancellationToken = default);
}
