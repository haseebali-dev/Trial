namespace CryptoTrading.API.Models;

public class TradeJournal
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // BUY, SELL
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public string SetupType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ScreenshotPath { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // OPEN, WIN, LOSS, BREAKEVEN
    public decimal? PnL { get; set; }
    public decimal? RMultiple { get; set; }
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}