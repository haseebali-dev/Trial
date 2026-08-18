using System.Text.Json;
using CryptoTrading.API.Configuration;
using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using CryptoTrading.API.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.MarketData;

public class BinanceMarketDataProvider : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BinanceMarketDataProvider> _logger;
    private readonly MarketDataSettings _settings;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(100); // 10 req/sec
    private DateTime _lastRequest = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public BinanceMarketDataProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<BinanceMarketDataProvider> logger,
        IOptions<MarketDataSettings> settings)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _settings = settings.Value;
        _rateLimiter = new SemaphoreSlim(1, 1);

        _httpClient.BaseAddress = new Uri(_settings.BinanceApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<MarketTickerDto> GetTickerAsync(string symbol)
    {
        var cacheKey = $"ticker:{symbol.ToUpper()}";

        if (_cache.TryGetValue(cacheKey, out MarketTickerDto? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/v3/ticker/24hr?symbol={symbol.ToUpper()}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var binanceTicker = JsonSerializer.Deserialize<BinanceTickerResponse>(json, JsonOptions);

            if (binanceTicker == null)
            {
                throw new InvalidOperationException("Failed to deserialize ticker response");
            }

            var ticker = new MarketTickerDto(
                Symbol: binanceTicker.Symbol,
                Price: decimal.Parse(binanceTicker.LastPrice),
                Volume24h: decimal.Parse(binanceTicker.Volume),
                Change24h: decimal.Parse(binanceTicker.PriceChangePercent),
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                High24h: decimal.TryParse(binanceTicker.HighPrice, out var high) ? high : null,
                Low24h: decimal.TryParse(binanceTicker.LowPrice, out var low) ? low : null,
                Open24h: decimal.TryParse(binanceTicker.OpenPrice, out var open) ? open : null
            );

            _cache.Set(cacheKey, ticker, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return ticker;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch ticker for {Symbol}", symbol);
            throw new InvalidOperationException($"Market data unavailable for {symbol}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse ticker response for {Symbol}", symbol);
            throw new InvalidOperationException($"Invalid market data format for {symbol}", ex);
        }
    }

    public async Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, int limit = 500)
    {
        var cacheKey = $"candles:{symbol.ToUpper()}:{timeframe}:{limit}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CandleDto>? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        var interval = MapTimeframe(timeframe);
        if (interval == null)
        {
            throw new ArgumentException($"Unsupported timeframe: {timeframe}");
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v3/klines?symbol={symbol.ToUpper()}&interval={interval}&limit={Math.Min(limit, 1000)}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var klines = JsonSerializer.Deserialize<JsonElement[][]>(json, JsonOptions);

            if (klines == null)
            {
                return Array.Empty<CandleDto>();
            }

            var candles = klines.Select(k => new CandleDto(
                Timestamp: k[0].GetInt64(),
                Open: decimal.Parse(k[1].GetString()!),
                High: decimal.Parse(k[2].GetString()!),
                Low: decimal.Parse(k[3].GetString()!),
                Close: decimal.Parse(k[4].GetString()!),
                Volume: decimal.Parse(k[5].GetString()!)
            )).ToList();

            _cache.Set(cacheKey, candles, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return candles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch candles for {Symbol} {Timeframe}", symbol, timeframe);
            throw new InvalidOperationException($"Candle data unavailable for {symbol} {timeframe}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse candles response for {Symbol} {Timeframe}", symbol, timeframe);
            throw new InvalidOperationException($"Invalid candle data format for {symbol} {timeframe}", ex);
        }
    }

    public async Task<IReadOnlyList<MarketOverviewDto>> GetMarketOverviewAsync()
    {
        const string cacheKey = "market:overview";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MarketOverviewDto>? cached))
        {
            return cached!;
        }

        await EnforceRateLimitAsync();

        try
        {
            var response = await _httpClient.GetAsync("/api/v3/ticker/24hr");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tickers = JsonSerializer.Deserialize<BinanceTickerResponse[]>(json, JsonOptions);

            if (tickers == null)
            {
                return Array.Empty<MarketOverviewDto>();
            }

            // Filter to USDT pairs and sort by volume
            var overviews = tickers
                .Where(t => t.Symbol.EndsWith("USDT") && !t.Symbol.Contains("BULL") && !t.Symbol.Contains("BEAR"))
                .OrderByDescending(t => decimal.Parse(t.QuoteVolume))
                .Take(50)
                .Select(t => new MarketOverviewDto(
                    Symbol: t.Symbol,
                    Price: decimal.Parse(t.LastPrice),
                    Change24h: decimal.Parse(t.PriceChangePercent),
                    Volume24h: decimal.Parse(t.QuoteVolume)
                ))
                .ToList();

            _cache.Set(cacheKey, overviews, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return overviews;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch market overview");
            throw new InvalidOperationException("Market overview unavailable", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse market overview response");
            throw new InvalidOperationException("Invalid market overview format", ex);
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await EnforceRateLimitAsync();
            var response = await _httpClient.GetAsync("/api/v3/ping");
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

    private static string? MapTimeframe(string timeframe)
    {
        return timeframe.ToLower() switch
        {
            "1m" => "1m",
            "5m" => "5m",
            "15m" => "15m",
            "30m" => "30m",
            "1h" => "1h",
            "4h" => "4h",
            "1d" => "1d",
            _ => null
        };
    }

    private class BinanceTickerResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public string LastPrice { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string QuoteVolume { get; set; } = string.Empty;
        public string PriceChangePercent { get; set; } = string.Empty;
        public string HighPrice { get; set; } = string.Empty;
        public string LowPrice { get; set; } = string.Empty;
        public string OpenPrice { get; set; } = string.Empty;
    }
}