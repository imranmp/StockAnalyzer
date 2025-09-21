using CsvHelper;
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
    private readonly IAnalysisFetcher _analysisFetcher;
    private readonly JsonSerializerOptions options;

    public FileStorage(ILogger<FileStorage> logger, IAnalysisFetcher analysisFetcher, string? analysisFile = null)
    {
        _logger = logger;
        _analysisFile = string.IsNullOrWhiteSpace(analysisFile) ? DefaultAnalysisFile : analysisFile;
        _analysisFetcher = analysisFetcher;

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

    public async Task<List<Analysis>> SaveAnalysisAsync(List<(string ticker, string companyName)> tickers,
                                                        List<Analysis> analyses,
                                                        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var (ticker, companyName) in tickers)
            {
                _logger.LogInformation("Processing {Ticker}", ticker);

                var results = await _analysisFetcher.FetchAnalysisAsync(ticker, cancellationToken);
                if (results != null && results.Count != 0)
                {
                    foreach (var entry in results)
                    {
                        var existing = analyses.FirstOrDefault(a => a.Symbol == entry.Symbol && a.Period == entry.Period);
                        if (existing != null)
                        {
                            // replace
                            analyses.Remove(existing);
                            analyses.Add(entry);
                        }
                        else
                        {
                            analyses.Add(entry);
                        }
                    }
                }
            }

            analyses = [.. analyses.OrderBy(a => a.Symbol).ThenByDescending(a => a.Period)];

            var json = JsonSerializer.Serialize(analyses, options);
            await File.WriteAllTextAsync(_analysisFile, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analysis to file");
        }

        return analyses;
    }

    public void OpenWriteStream(string relativePath, List<Analysis> analyses)
    {
        try
        {
            using var csvStream = File.Open(relativePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };
            using var writer = new StreamWriter(csvStream);
            using var csv = new CsvWriter(writer, config);
            csv.WriteRecords(analyses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open write stream for {Path}", relativePath);
        }
    }
}
