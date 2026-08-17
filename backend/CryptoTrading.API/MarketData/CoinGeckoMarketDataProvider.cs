using System.Text.Json;
using CryptoTrading.API.Configuration;
using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.MarketData;

public class CoinGeckoMarketDataProvider : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CoinGeckoMarketDataProvider> _logger;
    private readonly MarketDataSettings _settings;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly TimeSpan _rateLimitDelay = TimeSpan.FromSeconds(1.2); // ~50 req/min
    private DateTime _lastRequest = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Symbol mapping from Binance format to CoinGecko IDs
    private static readonly Dictionary<string, string> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTCUSDT"] = "bitcoin",
        ["ETHUSDT"] = "ethereum",
        ["SOLUSDT"] = "solana",
        ["BNBUSDT"] = "binancecoin",
        ["XRPUSDT"] = "ripple",
        ["ADAUSDT"] = "cardano",
        ["DOGEUSDT"] = "dogecoin",
        ["AVAXUSDT"] = "avalanche-2",
        ["DOTUSDT"] = "polkadot",
        ["LINKUSDT"] = "chainlink",
        ["MATICUSDT"] = "matic-network",
        ["LTCUSDT"] = "litecoin",
        ["UNIUSDT"] = "uniswap",
        ["ATOMUSDT"] = "cosmos",
        ["ETCUSDT"] = "ethereum-classic"
    };

    public CoinGeckoMarketDataProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CoinGeckoMarketDataProvider> logger,
        IOptions<MarketDataSettings> settings)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _settings = settings.Value;
        _rateLimiter = new SemaphoreSlim(1, 1);

        _httpClient.BaseAddress = new Uri(_settings.CoinGeckoApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<MarketTickerDto> GetTickerAsync(string symbol)
    {
        var cacheKey = $"cg:ticker:{symbol.ToUpper()}";

        if (_cache.TryGetValue(cacheKey, out MarketTickerDto? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        if (!SymbolMap.TryGetValue(symbol.ToUpper(), out var coinId))
        {
            throw new ArgumentException($"Symbol {symbol} not supported by CoinGecko");
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"/coins/{coinId}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var coinData = JsonSerializer.Deserialize<CoinGeckoCoinResponse>(json, JsonOptions);

            if (coinData?.MarketData == null)
            {
                throw new InvalidOperationException("No market data in response");
            }

            var ticker = new MarketTickerDto(
                Symbol: symbol.ToUpper(),
                Price: coinData.MarketData.CurrentPrice.Usd,
                Volume24h: coinData.MarketData.TotalVolume.Usd,
                Change24h: coinData.MarketData.PriceChangePercentage24h ?? 0,
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                High24h: coinData.MarketData.High24h?.Usd,
                Low24h: coinData.MarketData.Low24h?.Usd,
                Open24h: coinData.MarketData.CurrentPrice.Usd // Approximation
            );

            _cache.Set(cacheKey, ticker, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return ticker;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch ticker from CoinGecko for {Symbol}", symbol);
            throw new InvalidOperationException($"CoinGecko data unavailable for {symbol}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse CoinGecko ticker response for {Symbol}", symbol);
            throw new InvalidOperationException($"Invalid CoinGecko data format for {symbol}", ex);
        }
    }

    public async Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, int limit = 500)
    {
        var cacheKey = $"cg:candles:{symbol.ToUpper()}:{timeframe}:{limit}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CandleDto>? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        if (!SymbolMap.TryGetValue(symbol.ToUpper(), out var coinId))
        {
            throw new ArgumentException($"Symbol {symbol} not supported by CoinGecko");
        }

        var days = MapTimeframeToDays(timeframe, limit);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/coins/{coinId}/market_chart?vs_currency=usd&days={days}&interval={MapInterval(timeframe)}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var chartData = JsonSerializer.Deserialize<CoinGeckoChartResponse>(json, JsonOptions);

            if (chartData?.Prices == null)
            {
                return Array.Empty<CandleDto>();
            }

            // CoinGecko returns [timestamp, price] arrays - we need to convert to OHLCV
            // For simplicity, we'll create candles from price data
            // Note: CoinGecko free tier doesn't provide OHLCV directly, only prices and volumes
            var candles = CreateCandlesFromPrices(chartData.Prices, chartData.TotalVolumes, timeframe, limit);

            _cache.Set(cacheKey, candles, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return candles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch candles from CoinGecko for {Symbol} {Timeframe}", symbol, timeframe);
            throw new InvalidOperationException($"CoinGecko candle data unavailable for {symbol} {timeframe}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse CoinGecko candles response for {Symbol} {Timeframe}", symbol, timeframe);
            throw new InvalidOperationException($"Invalid CoinGecko candle data format for {symbol} {timeframe}", ex);
        }
    }

    public async Task<IReadOnlyList<MarketOverviewDto>> GetMarketOverviewAsync()
    {
        const string cacheKey = "cg:market:overview";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MarketOverviewDto>? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        try
        {
            var ids = string.Join(",", SymbolMap.Values);
            var response = await _httpClient.GetAsync(
                $"/coins/markets?vs_currency=usd&ids={ids}&order=market_cap_desc&per_page=50&page=1&sparkline=false&price_change_percentage=24h");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var markets = JsonSerializer.Deserialize<CoinGeckoMarketResponse[]>(json, JsonOptions);

            if (markets == null)
            {
                return Array.Empty<MarketOverviewDto>();
            }

            var overviews = markets
                .Select(m => new MarketOverviewDto(
                    Symbol: GetSymbolFromId(m.Id),
                    Price: m.CurrentPrice,
                    Change24h: m.PriceChangePercentage24h ?? 0,
                    Volume24h: m.TotalVolume
                ))
                .ToList();

            _cache.Set(cacheKey, overviews, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return overviews;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch market overview from CoinGecko");
            throw new InvalidOperationException("CoinGecko market overview unavailable", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse CoinGecko market overview response");
            throw new InvalidOperationException("Invalid CoinGecko market overview format", ex);
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await EnforceRateLimitAsync();
            var response = await _httpClient.GetAsync("/ping");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnforceRateLimitAsync()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequest;
            if (elapsed < _rateLimitDelay)
            {
                await Task.Delay(_rateLimitDelay - elapsed);
            }
            _lastRequest = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static int MapTimeframeToDays(string timeframe, int limit)
    {
        return timeframe.ToLower() switch
        {
            "1m" => Math.Min(1, (limit + 1439) / 1440),
            "5m" => Math.Min(1, (limit * 5 + 1439) / 1440),
            "15m" => Math.Min(7, (limit * 15 + 1439) / 1440),
            "30m" => Math.Min(7, (limit * 30 + 1439) / 1440),
            "1h" => Math.Min(30, (limit + 23) / 24),
            "4h" => Math.Min(90, (limit * 4 + 23) / 24),
            "1d" => Math.Min(365, limit),
            _ => 7
        };
    }

    private static string MapInterval(string timeframe)
    {
        return timeframe.ToLower() switch
        {
            "1m" => "hourly",
            "5m" => "hourly",
            "15m" => "hourly",
            "30m" => "hourly",
            "1h" => "hourly",
            "4h" => "daily",
            "1d" => "daily",
            _ => "hourly"
        };
    }

    private static List<CandleDto> CreateCandlesFromPrices(
        List<List<double>> prices,
        List<List<double>> volumes,
        string timeframe,
        int limit)
    {
        // CoinGecko returns [timestamp_ms, value] pairs
        // We'll create approximate candles by grouping prices
        var candles = new List<CandleDto>();

        if (prices.Count == 0)
            return candles;

        var intervalMs = GetIntervalMs(timeframe);
        var grouped = new Dictionary<long, (decimal Open, decimal High, decimal Low, decimal Close, decimal Volume)>();

        for (int i = 0; i < prices.Count; i++)
        {
            var timestamp = (long)prices[i][0];
            var price = (decimal)prices[i][1];
            var volume = i < volumes.Count ? (decimal)volumes[i][1] : 0;

            var bucket = (timestamp / intervalMs) * intervalMs;

            if (!grouped.ContainsKey(bucket))
            {
                grouped[bucket] = (price, price, price, price, volume);
            }
            else
            {
                var current = grouped[bucket];
                grouped[bucket] = (
                    current.Open,
                    Math.Max(current.High, price),
                    Math.Min(current.Low, price),
                    price,
                    current.Volume + volume
                );
            }
        }

        var sortedBuckets = grouped.Keys.OrderBy(k => k).ToList();
        var startIndex = Math.Max(0, sortedBuckets.Count - limit);

        for (int i = startIndex; i < sortedBuckets.Count; i++)
        {
            var bucket = sortedBuckets[i];
            var data = grouped[bucket];
            candles.Add(new CandleDto(bucket, data.Open, data.High, data.Low, data.Close, data.Volume));
        }

        return candles;
    }

    private static long GetIntervalMs(string timeframe)
    {
        return timeframe.ToLower() switch
        {
            "1m" => 60_000,
            "5m" => 300_000,
            "15m" => 900_000,
            "30m" => 1_800_000,
            "1h" => 3_600_000,
            "4h" => 14_400_000,
            "1d" => 86_400_000,
            _ => 3_600_000
        };
    }

    private static string GetSymbolFromId(string id)
    {
        var entry = SymbolMap.FirstOrDefault(kvp => kvp.Value == id);
        return entry.Key ?? id.ToUpper();
    }

    private class CoinGeckoCoinResponse
    {
        public CoinGeckoMarketData? MarketData { get; set; }
    }

    private class CoinGeckoMarketData
    {
        public CoinGeckoPrice CurrentPrice { get; set; } = new();
        public CoinGeckoPrice TotalVolume { get; set; } = new();
        public decimal? PriceChangePercentage24h { get; set; }
        public CoinGeckoPrice High24h { get; set; } = new();
        public CoinGeckoPrice Low24h { get; set; } = new();
    }

    private class CoinGeckoPrice
    {
        public decimal Usd { get; set; }
    }

    private class CoinGeckoChartResponse
    {
        public List<List<double>> Prices { get; set; } = new();
        public List<List<double>> TotalVolumes { get; set; } = new();
    }

    private class CoinGeckoMarketResponse
    {
        public string Id { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal? PriceChangePercentage24h { get; set; }
    }
}