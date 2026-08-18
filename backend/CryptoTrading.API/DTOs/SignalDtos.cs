namespace CryptoTrading.API.DTOs;

public record SignalDto(
    string Symbol,
    string Timeframe,
    string Type,
    string Direction,
    decimal Confidence,
    decimal EntryPrice,
    decimal? StopLoss,
    decimal? TakeProfit,
    string Strategy,
    string Reason,
    Dictionary<string, object> Metadata,
    long Timestamp
);

public record SignalSummaryDto(
    string Symbol,
    string Timeframe,
    decimal OverallScore,
    string OverallDirection,
    IReadOnlyList<SignalDto> Signals,
    Dictionary<string, decimal> StrategyScores,
    long Timestamp
);

public record GetSignalsRequest(
    string Symbol,
    string Timeframe,
    string[]? Strategies = null
);
