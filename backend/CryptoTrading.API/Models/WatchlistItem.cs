namespace CryptoTrading.API.Models;

public class WatchlistItem
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsFavorite { get; set; } = false;
    public string DefaultTimeframe { get; set; } = "1h";
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}