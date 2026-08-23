namespace CryptoTrading.API.DTOs;

public record ScanResultDto(
    string Symbol,
    string Timeframe,
    decimal OverallScore,
    string OverallDirection,
    int SignalCount,
    decimal TrendFollowingScore,
    decimal MeanReversionScore,
    decimal BreakoutScore,
    decimal MomentumScore,
    decimal SmcScore,
    decimal PatternScore,
    string TopSignalType,
    string TopSignalDirection,
    decimal TopSignalConfidence,
    long Timestamp
);

public record ScannerSummaryDto(
    int TotalSymbolsScanned,
    int BullishSymbols,
    int BearishSymbols,
    int NeutralSymbols,
    IReadOnlyList<ScanResultDto> TopOpportunities,
    IReadOnlyList<ScanResultDto> TopRisks,
    long ScanTimestamp,
    TimeSpan ScanDuration
);

public record ScanRequestDto(
    List<string>? Symbols = null,
    List<string>? Timeframes = null,
    decimal MinimumScore = 5,
    int MaxConcurrentScans = 5
);

public record SymbolScanDetailsDto(
    string Symbol,
    string Timeframe,
    ScanResultDto ScanResult,
    SignalSummaryDto Signals,
    SmcAnalysisResponse SmcAnalysis,
    PatternRecognitionResponse Patterns,
    IndicatorsResponse Indicators,
    MarketTickerDto Ticker
);