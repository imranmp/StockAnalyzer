namespace StockAnalyzer.Models;

public class Company
{
    public required string Name { get; set; }
    
    public required string Ticker { get; set; }

    public Analysis[]? Analyses { get; set; }
}