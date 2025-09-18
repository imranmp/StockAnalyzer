namespace StockAnalyzer.Models;

public class Analysis
{
    public string Symbol { get; set; } = null!;
    public int StrongSell { get; set; }
    public int Sell { get; set; }
    public int Hold { get; set; }
    public int Buy { get; set; }
    public int StrongBuy { get; set; }
    public DateOnly Period { get; set; }

    public int TotalAnalysis => StrongSell + Sell + Hold + Buy + StrongBuy;
    public int TotalSell => StrongSell + Sell;
    public int TotalBuy => StrongBuy + Buy;
    
    public decimal PercentageSell => TotalAnalysis == 0 ? 0 : TotalSell / (decimal)TotalAnalysis;
    public decimal PercentageBuy => TotalAnalysis == 0 ? 0 : TotalBuy / (decimal)TotalAnalysis;
    
    public decimal PercentageStrongSell => TotalSell == 0 ? 0 : StrongSell / (decimal)TotalSell;
    public decimal PercentageStrongBuy => TotalBuy == 0 ? 0 : StrongBuy / (decimal)TotalBuy;
    
    public decimal Score => CalculateScore();

    public decimal CalculateScore()
    {
        // Assign weights to each category
        decimal strongSellWeight = -1.0m;
        decimal sellWeight = -0.5m;
        decimal holdWeight = 0m;
        decimal buyWeight = 0.5m;
        decimal strongBuyWeight = 1.0m;

        // Calculate the weighted sum
        decimal weightedSum = (StrongSell * strongSellWeight) +
                             (Sell * sellWeight) +
                             (Hold * holdWeight) +
                             (Buy * buyWeight) +
                             (StrongBuy * strongBuyWeight);

        // Avoid division by zero
        if (TotalAnalysis == 0)
        {
            return 0;
        }

        // Calculate the weighted average score
        return weightedSum / TotalAnalysis;
    }
}