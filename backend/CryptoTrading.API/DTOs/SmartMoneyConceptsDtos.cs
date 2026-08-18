namespace CryptoTrading.API.DTOs;

public enum OrderBlockType
{
    Bullish,
    Bearish
}

public enum LiquidityType
{
    BuyLiquidity,  // Above resistance
    SellLiquidity  // Below support
}

public enum MarketStructure
{
    Bullish,
    Bearish,
    Ranging
}

public enum StructureBreakType
{
    BOS,    // Break of Structure (continuation)
    CHoCH   // Change of Character (reversal)
}

public enum ZoneType
{
    Premium,
    Equilibrium,
    Discount
}

public record OrderBlockDto(
    long StartTimestamp,
    long EndTimestamp,
    OrderBlockType Type,
    decimal HighPrice,
    decimal LowPrice,
    decimal Strength,
    bool IsMitigated,
    string Description,
    Dictionary<string, object> Metadata
);

public record FairValueGapDto(
    long StartTimestamp,
    long EndTimestamp,
    OrderBlockType Type,
    decimal TopPrice,
    decimal BottomPrice,
    decimal GapSize,
    bool IsFilled,
    decimal FilledPercentage,
    string Description,
    Dictionary<string, object> Metadata
);

public record LiquidityZoneDto(
    long Timestamp,
    LiquidityType Type,
    decimal Price,
    decimal Strength,
    bool IsSwept,
    string Description,
    Dictionary<string, object> Metadata
);

public record StructureBreakDto(
    long Timestamp,
    StructureBreakType Type,
    MarketStructure PreviousStructure,
    MarketStructure NewStructure,
    decimal BreakPrice,
    decimal PreviousHigh,
    decimal PreviousLow,
    string Description,
    Dictionary<string, object> Metadata
);

public record PremiumDiscountZoneDto(
    long StartTimestamp,
    long EndTimestamp,
    ZoneType Type,
    decimal HighPrice,
    decimal LowPrice,
    decimal CurrentPrice,
    string Description
);

public record SmcAnalysisResponse(
    string Symbol,
    string Timeframe,
    MarketStructure CurrentStructure,
    IReadOnlyList<OrderBlockDto> OrderBlocks,
    IReadOnlyList<FairValueGapDto> FairValueGaps,
    IReadOnlyList<LiquidityZoneDto> LiquidityZones,
    IReadOnlyList<StructureBreakDto> StructureBreaks,
    PremiumDiscountZoneDto? PremiumDiscountZone,
    decimal CurrentPrice,
    long LastUpdated
);

public record SmcSignalDto(
    string Symbol,
    string Timeframe,
    string SignalType,
    string Direction,
    decimal EntryPrice,
    decimal? StopLoss,
    decimal? TakeProfit,
    decimal Confidence,
    string Reason,
    Dictionary<string, object> Metadata,
    long Timestamp
);