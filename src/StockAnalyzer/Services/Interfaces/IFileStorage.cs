using StockAnalyzer.Models;

namespace StockAnalyzer.Services.Interfaces;

public interface IFileStorage
{
    Task<List<Analysis>> LoadAnalysisAsync(CancellationToken cancellationToken = default);

    Task<List<Analysis>> SaveAnalysisAsync(List<(string ticker, string companyName)> tickers, List<Analysis> analyses, CancellationToken cancellationToken = default);

    void OpenWriteStream(string relativePath, List<Analysis> analyses);
}
