using Microsoft.Extensions.Logging;
using StockAnalyzer.Models;
using StockAnalyzer.Services.Interfaces;
using System.Text.Json;

namespace StockAnalyzer.Services;

public class AnalysisFetcher : IAnalysisFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnalysisFetcher> _logger;
    private readonly JsonSerializerOptions serializerOptions;

    public AnalysisFetcher(IHttpClientFactory httpClientFactory, ILogger<AnalysisFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        serializerOptions = new()
        {
            Converters = { new Converters.DecimalConverter(), new Converters.DoubleConverter() },
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Analysis>?> FetchAnalysisAsync(string ticker, CancellationToken cancellationToken = default)
    {
        // Use named client configured with resilience pipeline
        var client = _httpClientFactory.CreateClient("Finnhub");

        try
        {
            // request path relative to BaseAddress
            var response = await client.GetAsync($"api/v1/stock/recommendation?symbol={ticker}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch analysis for {Ticker}: {Status} - {Body}",
                    ticker, response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<List<Analysis>>(body, serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching analysis for {Ticker}", ticker);
            return null;
        }
    }
}
