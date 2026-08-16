namespace CryptoTrading.API.Models;

public class Backtest
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MinimumScore { get; set; }
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal NetPnL { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal AverageR { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}