using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface ISignalGenerationService
{
    Task<SignalSummaryDto> GetSignalsAsync(string symbol, string timeframe, string[]? strategies = null);
    Task<IReadOnlyList<SignalDto>> GetTrendFollowingSignalsAsync(string symbol, string timeframe);
    Task<IReadOnlyList<SignalDto>> GetMeanReversionSignalsAsync(string symbol, string timeframe);
    Task<IReadOnlyList<SignalDto>> GetBreakoutSignalsAsync(string symbol, string timeframe);
    Task<IReadOnlyList<SignalDto>> GetMomentumSignalsAsync(string symbol, string timeframe);
}
