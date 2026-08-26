using System.Text.Json.Serialization;

namespace CryptoTrading.API.AI;

public interface IAIProvider
{
    Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisRequest request);
    Task<IEnumerable<string>> GetAvailableModelsAsync();
    Task<bool> IsHealthyAsync();
    string ProviderName { get; }
}

public class AIAnalysisRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1h";
    public MarketContext MarketContext { get; set; } = new();
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.3f;
}

public class MarketContext
{
    public decimal CurrentPrice { get; set; }
    public decimal Change24h { get; set; }
    public decimal Volume24h { get; set; }
    public MarketBias Bias1h { get; set; }
    public MarketBias Bias4h { get; set; }
    public MarketBias Bias1d { get; set; }
    public TechnicalIndicators Indicators { get; set; } = new();
    public List<SupportResistanceLevel> SupportResistance { get; set; } = new();
    public List<FVGLevel> FVGs { get; set; } = new();
    public List<OrderBlockLevel> OrderBlocks { get; set; } = new();
    public List<CandleDto> RecentCandles { get; set; } = new();
}

public class MarketBias
{
    public string Direction { get; set; } = "NEUTRAL";
    public int Strength { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
}

public class TechnicalIndicators
{
    public RSIData RSI { get; set; } = new();
    public VolumeData Volume { get; set; } = new();
    public EMAData EMA { get; set; } = new();
    public MACDData MACD { get; set; } = new();
    public ATRData ATR { get; set; } = new();
}

public class RSIData
{
    public decimal Value { get; set; }
    public string Signal { get; set; } = "NEUTRAL";
    public bool IsOverbought { get; set; }
    public bool IsOversold { get; set; }
    public bool BullishConfirmation { get; set; }
    public bool BearishConfirmation { get; set; }
}

public class VolumeData
{
    public decimal CurrentVolume { get; set; }
    public decimal AverageVolume { get; set; }
    public decimal VolumeRatio { get; set; }
    public bool VolumeSpike { get; set; }
    public bool BullishConfirmation { get; set; }
    public bool BearishConfirmation { get; set; }
}

public class EMAData
{
    public decimal EMA9 { get; set; }
    public decimal EMA21 { get; set; }
    public decimal EMA50 { get; set; }
    public string Trend { get; set; } = "NEUTRAL";
    public bool BullishAlignment { get; set; }
    public bool BearishAlignment { get; set; }
}

public class MACDData
{
    public decimal MACD { get; set; }
    public decimal Signal { get; set; }
    public decimal Histogram { get; set; }
    public string Trend { get; set; } = "NEUTRAL";
}

public class ATRData
{
    public decimal Value { get; set; }
    public decimal StopLossDistance { get; set; }
}

public class SupportResistanceLevel
{
    public decimal Price { get; set; }
    public string Type { get; set; } = "SUPPORT"; // SUPPORT, RESISTANCE, STRONG_SUPPORT, STRONG_RESISTANCE
    public int Touches { get; set; }
    public string Timeframe { get; set; } = "1h";
    public decimal Strength { get; set; }
}

public class FVGLevel
{
    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public string Direction { get; set; } = "BULLISH";
    public bool Mitigated { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderBlockLevel
{
    public decimal ZoneTop { get; set; }
    public decimal ZoneBottom { get; set; }
    public string Direction { get; set; } = "BULLISH";
    public int Strength { get; set; }
    public bool Mitigated { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CandleDto
{
    public long Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

public class AIAnalysisResult
{
    [JsonPropertyName("decision")]
    public string Decision { get; set; } = "WAIT"; // BUY, SELL, WAIT

    [JsonPropertyName("confidence")]
    public int Confidence { get; set; } // 0-100

    [JsonPropertyName("marketCondition")]
    public string MarketCondition { get; set; } = "UNCLEAR"; // TRENDING, RANGING, VOLATILE, UNCLEAR

    [JsonPropertyName("reasons")]
    public string[] Reasons { get; set; } = Array.Empty<string>();

    [JsonPropertyName("risks")]
    public string[] Risks { get; set; } = Array.Empty<string>();

    [JsonPropertyName("entryValid")]
    public bool EntryValid { get; set; }

    [JsonPropertyName("stopLossValid")]
    public bool StopLossValid { get; set; }

    [JsonPropertyName("riskRewardValid")]
    public bool RiskRewardValid { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("entryPrice")]
    public decimal? EntryPrice { get; set; }

    [JsonPropertyName("stopLoss")]
    public decimal? StopLoss { get; set; }

    [JsonPropertyName("takeProfit1")]
    public decimal? TakeProfit1 { get; set; }

    [JsonPropertyName("takeProfit2")]
    public decimal? TakeProfit2 { get; set; }

    [JsonPropertyName("takeProfit3")]
    public decimal? TakeProfit3 { get; set; }

    [JsonPropertyName("riskReward")]
    public decimal? RiskReward { get; set; }
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;
}

public class OllamaModelsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.3f;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "json";
}

public class OllamaGenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("context")]
    public int[] Context { get; set; } = Array.Empty<int>();
}