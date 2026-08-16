namespace CryptoTrading.API.Configuration;

public class MarketDataSettings
{
    public string DefaultExchange { get; set; } = "binance";
    public string DefaultSymbol { get; set; } = "BTCUSDT";
    public string DefaultTimeframe { get; set; } = "1h";
    public int RefreshRateSeconds { get; set; } = 30;
    public int CacheDurationSeconds { get; set; } = 60;
    public int MaxCandles { get; set; } = 500;
    public string CoinGeckoApiUrl { get; set; } = "https://api.coingecko.com/api/v3";
    public string BinanceApiUrl { get; set; } = "https://api.binance.com";
    public string BinanceWsUrl { get; set; } = "wss://stream.binance.com:9443/ws";
}