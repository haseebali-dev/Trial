namespace CryptoTrading.API.Configuration;

public class ScannerSettings
{
    public List<string> DefaultSymbols { get; set; } = new()
    {
        "BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT", "XRPUSDT",
        "ADAUSDT", "DOGEUSDT", "AVAXUSDT", "DOTUSDT", "LINKUSDT"
    };
    public int ScanIntervalMinutes { get; set; } = 15;
    public int MinimumScore { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public int MaxConcurrentScans { get; set; } = 5;
}