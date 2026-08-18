namespace CryptoTrading.API.Models;

public class MarketSnapshot
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume24h { get; set; }
    public decimal Change24h { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}