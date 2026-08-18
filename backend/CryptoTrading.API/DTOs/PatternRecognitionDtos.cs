namespace CryptoTrading.API.DTOs;

public enum PatternType
{
    // Single Candle Patterns
    Doji,
    Hammer,
    InvertedHammer,
    ShootingStar,
    HangingMan,
    SpinningTop,
    Marubozu,
    LongLeggedDoji,
    DragonflyDoji,
    GravestoneDoji,

    // Two Candle Patterns
    BullishEngulfing,
    BearishEngulfing,
    BullishHarami,
    BearishHarami,
    TweezerTop,
    TweezerBottom,
    PiercingPattern,
    DarkCloudCover,

    // Three Candle Patterns
    MorningStar,
    EveningStar,
    ThreeWhiteSoldiers,
    ThreeBlackCrows,
    ThreeInsideUp,
    ThreeInsideDown,
    RisingThreeMethods,
    FallingThreeMethods,

    // Chart Patterns
    HeadAndShoulders,
    InverseHeadAndShoulders,
    DoubleTop,
    DoubleBottom,
    TripleTop,
    TripleBottom,
    AscendingTriangle,
    DescendingTriangle,
    SymmetricalTriangle,
    WedgeRising,
    WedgeFalling,
    Rectangle,
    FlagBullish,
    FlagBearish,
    PennantBullish,
    PennantBearish,
    CupAndHandle,
}

public enum PatternDirection
{
    Bullish,
    Bearish,
    Neutral,
    Reversal,
    Continuation
}

public record CandlePatternDto(
    long Timestamp,
    PatternType Type,
    PatternDirection Direction,
    string Name,
    decimal Confidence,
    string Description,
    Dictionary<string, object> Metadata
);

public record ChartPatternDto(
    long StartTimestamp,
    long EndTimestamp,
    PatternType Type,
    PatternDirection Direction,
    string Name,
    decimal Confidence,
    string Description,
    Dictionary<string, object> Metadata,
    decimal? BreakoutPrice,
    decimal? TargetPrice,
    decimal? StopLossPrice
);

public record PatternRecognitionResponse(
    string Symbol,
    string Timeframe,
    IReadOnlyList<CandlePatternDto> CandlePatterns,
    IReadOnlyList<ChartPatternDto> ChartPatterns,
    long LastUpdated
);

public record PatternAlertDto(
    string Symbol,
    string Timeframe,
    PatternType PatternType,
    PatternDirection Direction,
    decimal Confidence,
    string Message,
    long Timestamp
);