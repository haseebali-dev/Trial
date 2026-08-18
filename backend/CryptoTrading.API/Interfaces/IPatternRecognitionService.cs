using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface IPatternRecognitionService
{
    Task<PatternRecognitionResponse> GetPatternsAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<CandlePatternDto>> GetCandlePatternsAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<ChartPatternDto>> GetChartPatternsAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<PatternAlertDto>> GetRecentAlertsAsync(string symbol, string timeframe, int hours = 24);
}