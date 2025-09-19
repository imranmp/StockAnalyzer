using StockAnalyzer.Models;

namespace StockAnalyzer.Services.Interfaces;

public interface IFileStorage
{
    Task<List<Analysis>> LoadAnalysisAsync();
    Task SaveAnalysisAsync(List<Analysis> analyses);
    Stream OpenWriteStream(string relativePath);
}
