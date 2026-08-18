using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface ITechnicalAnalysisService
{
    Task<IndicatorsResponse> GetIndicatorsAsync(string symbol, string timeframe, int limit = 500, string[]? indicators = null);
    Task<IReadOnlyList<SmaDto>> GetSmaAsync(string symbol, string timeframe, int period, int limit = 500);
    Task<IReadOnlyList<EmaDto>> GetEmaAsync(string symbol, string timeframe, int period, int limit = 500);
    Task<IReadOnlyList<RsiDto>> GetRsiAsync(string symbol, string timeframe, int period = 14, int limit = 500);
    Task<IReadOnlyList<MacdDto>> GetMacdAsync(string symbol, string timeframe, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int limit = 500);
    Task<IReadOnlyList<BollingerBandsDto>> GetBollingerBandsAsync(string symbol, string timeframe, int period = 20, decimal stdDev = 2, int limit = 500);
    Task<IReadOnlyList<AtrDto>> GetAtrAsync(string symbol, string timeframe, int period = 14, int limit = 500);
    Task<IReadOnlyList<StochasticDto>> GetStochasticAsync(string symbol, string timeframe, int kPeriod = 14, int dPeriod = 3, int limit = 500);
    Task<IReadOnlyList<AdxDto>> GetAdxAsync(string symbol, string timeframe, int period = 14, int limit = 500);
    Task<IReadOnlyList<ObvDto>> GetObvAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<VwapDto>> GetVwapAsync(string symbol, string timeframe, int limit = 500);
}