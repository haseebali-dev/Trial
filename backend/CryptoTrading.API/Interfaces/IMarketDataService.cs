using CryptoTrading.API.Models;
using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface IMarketDataService
{
    Task<MarketTickerDto> GetTickerAsync(string symbol);
    Task<IReadOnlyList<CandleDto>> GetCandlesAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<MarketOverviewDto>> GetMarketOverviewAsync();
    Task<bool> IsHealthyAsync();
}