using CryptoTrading.API.Configuration;
using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.Services;

public class MarketDataService : IMarketDataService
{
    private readonly IMarketDataService _primaryProvider;
    private readonly IMarketDataService _fallbackProvider;
    private readonly ILogger<MarketDataService> _logger;
    private readonly MarketDataSettings _settings;

    public MarketDataService(
        IEnumerable<IMarketDataService> providers,
        ILogger<MarketDataService> logger,
        IOptions<MarketDataSettings> settings)
    {
        var providerList = providers.ToList();
        _primaryProvider = providerList.FirstOrDefault(p => p.GetType().Name.Contains("Binance"))
            ?? providerList.FirstOrDefault()
            ?? throw new InvalidOperationException("No market data provider registered");

        _fallbackProvider = providerList.FirstOrDefault(p => p != _primaryProvider)
            ?? _primaryProvider;

        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<MarketTickerDto> GetTickerAsync(string symbol)
    {
        try
        {
            return await _primaryProvider.GetTickerAsync(symbol);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary provider failed for {Symbol}, trying fallback", symbol);
            try
            {
                return await _fallbackProvider.GetTickerAsync(symbol);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback provider also failed for {Symbol}", symbol);
                throw new InvalidOperationException($"All market data providers failed for {symbol}", fallbackEx);
            }
        }
    }

    public async Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, int limit = 500)
    {
        try
        {
            return await _primaryProvider.GetCandlesAsync(symbol, timeframe, limit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary provider failed for {Symbol} {Timeframe}, trying fallback", symbol, timeframe);
            try
            {
                return await _fallbackProvider.GetCandlesAsync(symbol, timeframe, limit);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback provider also failed for {Symbol} {Timeframe}", symbol, timeframe);
                throw new InvalidOperationException($"All market data providers failed for {symbol} {timeframe}", fallbackEx);
            }
        }
    }

    public async Task<IReadOnlyList<MarketOverviewDto>> GetMarketOverviewAsync()
    {
        try
        {
            return await _primaryProvider.GetMarketOverviewAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary provider failed for market overview, trying fallback");
            try
            {
                return await _fallbackProvider.GetMarketOverviewAsync();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback provider also failed for market overview");
                throw new InvalidOperationException("All market data providers failed for market overview", fallbackEx);
            }
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        var primaryHealthy = await _primaryProvider.IsHealthyAsync();
        var fallbackHealthy = await _fallbackProvider.IsHealthyAsync();

        return primaryHealthy || fallbackHealthy;
    }
}