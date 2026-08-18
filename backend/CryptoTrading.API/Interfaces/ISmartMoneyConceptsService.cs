using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface ISmartMoneyConceptsService
{
    Task<SmcAnalysisResponse> GetSmcAnalysisAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<OrderBlockDto>> GetOrderBlocksAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<FairValueGapDto>> GetFairValueGapsAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<LiquidityZoneDto>> GetLiquidityZonesAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<StructureBreakDto>> GetStructureBreaksAsync(string symbol, string timeframe, int limit = 500);
    Task<IReadOnlyList<SmcSignalDto>> GetSmcSignalsAsync(string symbol, string timeframe, int limit = 500);
}