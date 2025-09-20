using StockAnalyzer.Models;

namespace StockAnalyzer.Services.Interfaces;

public interface IFileStorage
{
    Task<List<Analysis>> LoadAnalysisAsync(CancellationToken cancellationToken = default);

    Task SaveAnalysisAsync(List<Analysis> analyses, CancellationToken cancellationToken = default);

    Stream OpenWriteStream(string relativePath);
}
