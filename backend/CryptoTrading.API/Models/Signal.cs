namespace CryptoTrading.API.Models;

public class Signal
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // BUY, SELL, WAIT
    public int Score { get; set; }
    public int MaxScore { get; set; } = 7;
    public string ConfidenceLevel { get; set; } = string.Empty; // HIGH, MEDIUM, LOW
    public decimal? EntryPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit1 { get; set; }
    public decimal? TakeProfit2 { get; set; }
    public decimal? TakeProfit3 { get; set; }
    public decimal? RiskReward { get; set; }
    public string Reasons { get; set; } = string.Empty; // JSON serialized
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}