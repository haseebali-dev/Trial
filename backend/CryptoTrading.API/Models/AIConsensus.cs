namespace CryptoTrading.API.Models;

public class AIConsensus
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ConsensusDecision { get; set; } = string.Empty; // BUY, SELL, WAIT
    public int AgreeCount { get; set; }
    public int TotalModels { get; set; }
    public string ModelDecisions { get; set; } = string.Empty; // JSON serialized
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}