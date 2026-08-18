namespace CryptoTrading.API.DTOs;

public record IndicatorValueDto(
    long Timestamp,
    decimal? Value,
    decimal? UpperBand = null,
    decimal? LowerBand = null,
    decimal? Signal = null,
    decimal? Histogram = null
);

public record SmaDto(long Timestamp, decimal Value);
public record EmaDto(long Timestamp, decimal Value);
public record RsiDto(long Timestamp, decimal Value);
public record MacdDto(long Timestamp, decimal Macd, decimal Signal, decimal Histogram);
public record BollingerBandsDto(long Timestamp, decimal Upper, decimal Middle, decimal Lower, decimal PercentB, decimal Bandwidth);
public record AtrDto(long Timestamp, decimal Value);
public record StochasticDto(long Timestamp, decimal K, decimal D);
public record AdxDto(long Timestamp, decimal Adx, decimal PlusDi, decimal MinusDi);
public record ObvDto(long Timestamp, decimal Value);
public record VwapDto(long Timestamp, decimal Value);

public record IndicatorsResponse(
    string Symbol,
    string Timeframe,
    IReadOnlyList<SmaDto> Sma20,
    IReadOnlyList<SmaDto> Sma50,
    IReadOnlyList<SmaDto> Sma200,
    IReadOnlyList<EmaDto> Ema9,
    IReadOnlyList<EmaDto> Ema21,
    IReadOnlyList<EmaDto> Ema50,
    IReadOnlyList<RsiDto> Rsi14,
    IReadOnlyList<MacdDto> Macd,
    IReadOnlyList<BollingerBandsDto> BollingerBands20,
    IReadOnlyList<AtrDto> Atr14,
    IReadOnlyList<StochasticDto> Stochastic14,
    IReadOnlyList<AdxDto> Adx14,
    IReadOnlyList<ObvDto> Obv,
    IReadOnlyList<VwapDto> Vwap
);

public record GetIndicatorsRequest(
    string Symbol,
    string Timeframe,
    int Limit = 500,
    string[]? Indicators = null
);