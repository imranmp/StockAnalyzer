using Microsoft.Extensions.Logging;
using StockAnalyzer.Models;
using StockAnalyzer.Services.Interfaces;
using System.Text.Json;

namespace StockAnalyzer.Services;

public class FileStorage : IFileStorage
{
    private const string AnalysisFile = "AnalysisResult.json";

    private readonly ILogger<FileStorage> _logger;
    private readonly JsonSerializerOptions options;

    public FileStorage(ILogger<FileStorage> logger)
    {
        _logger = logger;

        options = new()
        {
            Converters = { new Converters.DecimalConverter(), new Converters.DoubleConverter() },
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Analysis>> LoadAnalysisAsync()
    {
        if (!File.Exists(AnalysisFile))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(AnalysisFile);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var list = JsonSerializer.Deserialize<List<Analysis>>(json, options);
            return list ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing analysis file");
            return new List<Analysis>();
        }
    }

    public async Task SaveAnalysisAsync(List<Analysis> analyses)
    {
        try
        {
            var json = JsonSerializer.Serialize(analyses, options);
            await File.WriteAllTextAsync(AnalysisFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analysis to file");
        }
    }

    public Stream OpenWriteStream(string relativePath)
    {
        try
        {
            return File.Open(relativePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open write stream for {Path}", relativePath);
            // fallback to memory stream to avoid crashing the app
            return new MemoryStream();
        }
    }
}
