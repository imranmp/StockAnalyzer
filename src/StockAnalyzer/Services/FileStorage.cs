using Microsoft.Extensions.Logging;
using StockAnalyzer.Models;
using StockAnalyzer.Services.Interfaces;
using System.Text.Json;

namespace StockAnalyzer.Services;

public class FileStorage : IFileStorage
{
    // analysis file name/path can be provided during construction (e.g. from args or DI)
    private const string DefaultAnalysisFile = "AnalysisResult.json";
    private readonly string _analysisFile;

    private readonly ILogger<FileStorage> _logger;
    private readonly JsonSerializerOptions options;

    public FileStorage(ILogger<FileStorage> logger, string? analysisFile = null)
    {
        _logger = logger;
        _analysisFile = string.IsNullOrWhiteSpace(analysisFile) ? DefaultAnalysisFile : analysisFile;

        options = new()
        {
            Converters = { new Converters.DecimalConverter(), new Converters.DoubleConverter() },
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Analysis>> LoadAnalysisAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_analysisFile))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(_analysisFile, cancellationToken);
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
            return [];
        }
    }

    public async Task SaveAnalysisAsync(List<Analysis> analyses, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(analyses, options);
            await File.WriteAllTextAsync(_analysisFile, json, cancellationToken);
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
