namespace CryptoTrading.API.Models;

public class AIAnalysis
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty; // BUY, SELL, WAIT
    public int Confidence { get; set; } // 0-100
    public string MarketCondition { get; set; } = string.Empty; // TRENDING, RANGING, VOLATILE, UNCLEAR
    public string Reasons { get; set; } = string.Empty; // JSON serialized
    public string Risks { get; set; } = string.Empty; // JSON serialized
    public bool EntryValid { get; set; }
    public bool StopLossValid { get; set; }
    public bool RiskRewardValid { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}