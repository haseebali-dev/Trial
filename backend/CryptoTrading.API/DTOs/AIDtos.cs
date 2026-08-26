using System.Text.Json.Serialization;

namespace CryptoTrading.API.DTOs;

public record TechnicalIndicatorsDto
{
    public RSIDataDto? RSI { get; set; }
    public VolumeDataDto? Volume { get; set; }
    public EMADataDto? EMA { get; set; }
    public MACDDataDto? MACD { get; set; }
    public ATRDataDto? ATR { get; set; }
}

public record RSIDataDto
{
    public decimal Value { get; set; }
    public string Signal { get; set; } = "NEUTRAL";
    public bool IsOverbought { get; set; }
    public bool IsOversold { get; set; }
    public bool BullishConfirmation { get; set; }
    public bool BearishConfirmation { get; set; }
}

public record VolumeDataDto
{
    public decimal CurrentVolume { get; set; }
    public decimal AverageVolume { get; set; }
    public decimal VolumeRatio { get; set; }
    public bool VolumeSpike { get; set; }
    public bool BullishConfirmation { get; set; }
    public bool BearishConfirmation { get; set; }
}

public record EMADataDto
{
    public decimal EMA9 { get; set; }
    public decimal EMA21 { get; set; }
    public decimal EMA50 { get; set; }
    public string Trend { get; set; } = "NEUTRAL";
    public bool BullishAlignment { get; set; }
    public bool BearishAlignment { get; set; }
}

public record MACDDataDto
{
    public decimal MACD { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
    public string Trend { get; set; } = "NEUTRAL";
}

public record ATRDataDto
{
    public decimal Value { get; set; }
    public decimal StopLossDistance { get; set; }
}

public record MarketBiasDto
{
    public string Direction { get; set; } = "NEUTRAL";
    public int Strength { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
}