using CryptoTrading.API.AI;
using CryptoTrading.API.Configuration;
using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly IAIConsensusService _consensusService;
    private readonly IMarketDataService _marketDataService;
    private readonly ITechnicalAnalysisService _technicalAnalysisService;
    private readonly ISmartMoneyConceptsService _smcService;
    private readonly IOptions<AISettings> _settings;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IEnumerable<IAIProvider> providers,
        IAIConsensusService consensusService,
        IMarketDataService marketDataService,
        ITechnicalAnalysisService technicalAnalysisService,
        ISmartMoneyConceptsService smcService,
        IOptions<AISettings> settings,
        ILogger<AIController> logger)
    {
        _providers = providers;
        _consensusService = consensusService;
        _marketDataService = marketDataService;
        _technicalAnalysisService = technicalAnalysisService;
        _smcService = smcService;
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("models")]
    public async Task<ActionResult<IEnumerable<string>>> GetModels()
    {
        try
        {
            var provider = _providers.FirstOrDefault();
            if (provider == null)
            {
                return BadRequest(new { error = "No AI provider configured" });
            }

            var models = await provider.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI models");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck()
    {
        try
        {
            var provider = _providers.FirstOrDefault();
            if (provider == null)
            {
                return BadRequest(new { error = "No AI provider configured" });
            }

            var isHealthy = await provider.IsHealthyAsync();
            return Ok(new
            {
                provider = provider.ProviderName,
                status = isHealthy ? "healthy" : "unhealthy",
                ollamaUrl = _settings.Value.OllamaBaseUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI health check failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AIAnalysisResult>> Analyze([FromBody] AnalyzeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                return BadRequest(new { error = "Symbol is required" });
            }

            var model = request.Model ?? _settings.Value.DefaultModel;

            // Build market context
            var context = await BuildMarketContextAsync(request.Symbol, request.Timeframe);

            // Single model analysis
            var result = await _consensusService.AnalyzeWithSingleModelAsync(
                request.Symbol,
                request.Timeframe,
                model,
                context);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("consensus")]
    public async Task<ActionResult<AIConsensusResult>> GetConsensus([FromBody] ConsensusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                return BadRequest(new { error = "Symbol is required" });
            }

            var models = request.Models?.Any() == true
                ? request.Models.ToArray()
                : new[] { _settings.Value.DefaultModel };

            // Build market context
            var context = await BuildMarketContextAsync(request.Symbol, request.Timeframe);

            // Get consensus from multiple models
            var result = await _consensusService.GetConsensusAsync(
                request.Symbol,
                request.Timeframe,
                models,
                context);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI consensus for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult> TestConnection([FromBody] TestConnectionRequest request)
    {
        try
        {
            var provider = _providers.FirstOrDefault();
            if (provider == null)
            {
                return BadRequest(new { error = "No AI provider configured" });
            }

            var isHealthy = await provider.IsHealthyAsync();
            var models = await provider.GetAvailableModelsAsync();

            var modelExists = models.Any(m => m == request.Model);

            return Ok(new
            {
                connected = isHealthy,
                provider = provider.ProviderName,
                ollamaUrl = _settings.Value.OllamaBaseUrl,
                modelAvailable = modelExists,
                availableModels = models,
                selectedModel = request.Model ?? _settings.Value.DefaultModel,
                responseTimeMs = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI connection test failed");
            return Ok(new
            {
                connected = false,
                error = ex.Message,
                provider = "Ollama",
                ollamaUrl = _settings.Value.OllamaBaseUrl
            });
        }
    }

    private async Task<MarketContext> BuildMarketContextAsync(string symbol, string timeframe)
    {
        var context = new MarketContext();

        try
        {
            // Get ticker
            var ticker = await _marketDataService.GetTickerAsync(symbol);
            if (ticker != null)
            {
                context.CurrentPrice = ticker.Price;
                context.Change24h = ticker.Change24h;
                context.Volume24h = ticker.Volume24h;
            }

            // Get candles for price action
            var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, 100);
            context.RecentCandles = candles.Select(c => new CryptoTrading.API.AI.CandleDto
            {
                Timestamp = c.Timestamp,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            }).ToList();

            // Get technical indicators
            var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, 100);
            if (indicators != null)
            {
                context.Indicators = MapIndicatorsFromResponse(indicators);
            }

            // Get SMC data (FVG, Order Blocks, S/R)
            var smcData = await _smcService.GetSmcAnalysisAsync(symbol, timeframe, 100);
            if (smcData != null)
            {
                // Convert OrderBlocks to Support/Resistance levels
                context.SupportResistance = smcData.OrderBlocks
                    .Where(ob => !ob.IsMitigated)
                    .Select(ob => new SupportResistanceLevel
                    {
                        Price = ob.Type == OrderBlockType.Bullish ? ob.LowPrice : ob.HighPrice,
                        Type = ob.Type == OrderBlockType.Bullish ? "SUPPORT" : "RESISTANCE",
                        Touches = 1,
                        Timeframe = timeframe,
                        Strength = (int)ob.Strength
                    })
                    .Concat(smcData.LiquidityZones
                        .Where(lz => !lz.IsSwept)
                        .Select(lz => new SupportResistanceLevel
                        {
                            Price = lz.Price,
                            Type = lz.Type == LiquidityType.BuyLiquidity ? "RESISTANCE" : "SUPPORT",
                            Touches = (int)lz.Metadata.GetValueOrDefault("testCount", 1),
                            Timeframe = timeframe,
                            Strength = (int)lz.Strength
                        }))
                    .ToList();

                context.FVGs = smcData.FairValueGaps.Select(fvg => new FVGLevel
                {
                    Top = fvg.TopPrice,
                    Bottom = fvg.BottomPrice,
                    Direction = fvg.Type == OrderBlockType.Bullish ? "BULLISH" : "BEARISH",
                    Mitigated = fvg.IsFilled,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(fvg.StartTimestamp).DateTime
                }).ToList();

                context.OrderBlocks = smcData.OrderBlocks.Select(ob => new OrderBlockLevel
                {
                    ZoneTop = ob.HighPrice,
                    ZoneBottom = ob.LowPrice,
                    Direction = ob.Type == OrderBlockType.Bullish ? "BULLISH" : "BEARISH",
                    Strength = (int)ob.Strength,
                    Mitigated = ob.IsMitigated,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(ob.StartTimestamp).DateTime
                }).ToList();
            }

            // Get market bias for multiple timeframes
            var bias1h = await _smcService.GetMarketBiasAsync(symbol, "1h");
            var bias4h = await _smcService.GetMarketBiasAsync(symbol, "4h");
            var bias1d = await _smcService.GetMarketBiasAsync(symbol, "1d");

            context.Bias1h = MapBias(bias1h);
            context.Bias4h = MapBias(bias4h);
            context.Bias1d = MapBias(bias1d);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building market context for {Symbol}", symbol);
        }

        return context;
    }

    private TechnicalIndicators MapIndicators(TechnicalIndicatorsDto dto)
    {
        return new TechnicalIndicators
        {
            RSI = new RSIData
            {
                Value = dto.RSI?.Value ?? 0,
                Signal = dto.RSI?.Signal ?? "NEUTRAL",
                IsOverbought = dto.RSI?.IsOverbought ?? false,
                IsOversold = dto.RSI?.IsOversold ?? false,
                BullishConfirmation = dto.RSI?.BullishConfirmation ?? false,
                BearishConfirmation = dto.RSI?.BearishConfirmation ?? false
            },
            Volume = new VolumeData
            {
                CurrentVolume = dto.Volume?.CurrentVolume ?? 0,
                AverageVolume = dto.Volume?.AverageVolume ?? 0,
                VolumeRatio = dto.Volume?.VolumeRatio ?? 0,
                VolumeSpike = dto.Volume?.VolumeSpike ?? false,
                BullishConfirmation = dto.Volume?.BullishConfirmation ?? false,
                BearishConfirmation = dto.Volume?.BearishConfirmation ?? false
            },
            EMA = new EMAData
            {
                EMA9 = dto.EMA?.EMA9 ?? 0,
                EMA21 = dto.EMA?.EMA21 ?? 0,
                EMA50 = dto.EMA?.EMA50 ?? 0,
                Trend = dto.EMA?.Trend ?? "NEUTRAL",
                BullishAlignment = dto.EMA?.BullishAlignment ?? false,
                BearishAlignment = dto.EMA?.BearishAlignment ?? false
            },
            MACD = new MACDData
            {
                MACD = dto.MACD?.MACD ?? 0,
                Signal = dto.MACD?.Signal ?? 0,
                Histogram = dto.MACD?.Histogram ?? 0,
                Trend = dto.MACD?.Trend ?? "NEUTRAL"
            },
            ATR = new ATRData
            {
                Value = dto.ATR?.Value ?? 0,
                StopLossDistance = dto.ATR?.StopLossDistance ?? 0
            }
        };
    }

    private MarketBias MapBias(MarketBiasDto? dto)
    {
        if (dto == null)
        {
            return new MarketBias { Direction = "NEUTRAL", Strength = 0, Reasons = Array.Empty<string>() };
        }

        return new MarketBias
        {
            Direction = dto.Direction,
            Strength = dto.Strength,
            Reasons = dto.Reasons ?? Array.Empty<string>()
        };
    }

    private TechnicalIndicators MapIndicatorsFromResponse(IndicatorsResponse dto)
    {
        var rsi = dto.Rsi14.LastOrDefault();
        var volume = dto.Obv.LastOrDefault();
        var ema9 = dto.Ema9.LastOrDefault();
        var ema21 = dto.Ema21.LastOrDefault();
        var ema50 = dto.Ema50.LastOrDefault();
        var macd = dto.Macd.LastOrDefault();
        var atr = dto.Atr14.LastOrDefault();
        var bb = dto.BollingerBands20.LastOrDefault();

        return new TechnicalIndicators
        {
            RSI = new RSIData
            {
                Value = rsi?.Value ?? 0,
                Signal = (rsi?.Value ?? 50) > 70 ? "OVERBOUGHT" : (rsi?.Value ?? 50) < 30 ? "OVERSOLD" : "NEUTRAL",
                IsOverbought = (rsi?.Value ?? 50) > 70,
                IsOversold = (rsi?.Value ?? 50) < 30,
                BullishConfirmation = (rsi?.Value ?? 50) > 50 && (rsi?.Value ?? 50) < 70,
                BearishConfirmation = (rsi?.Value ?? 50) < 50 && (rsi?.Value ?? 50) > 30
            },
            Volume = new VolumeData
            {
                CurrentVolume = volume?.Value ?? 0,
                AverageVolume = 0,
                VolumeRatio = 1,
                VolumeSpike = false,
                BullishConfirmation = false,
                BearishConfirmation = false
            },
            EMA = new EMAData
            {
                EMA9 = ema9?.Value ?? 0,
                EMA21 = ema21?.Value ?? 0,
                EMA50 = ema50?.Value ?? 0,
                Trend = (ema9?.Value ?? 0) > (ema21?.Value ?? 0) && (ema21?.Value ?? 0) > (ema50?.Value ?? 0) ? "BULLISH" :
                        (ema9?.Value ?? 0) < (ema21?.Value ?? 0) && (ema21?.Value ?? 0) < (ema50?.Value ?? 0) ? "BEARISH" : "NEUTRAL",
                BullishAlignment = (ema9?.Value ?? 0) > (ema21?.Value ?? 0) && (ema21?.Value ?? 0) > (ema50?.Value ?? 0),
                BearishAlignment = (ema9?.Value ?? 0) < (ema21?.Value ?? 0) && (ema21?.Value ?? 0) < (ema50?.Value ?? 0)
            },
            MACD = new MACDData
            {
                MACD = macd?.Macd ?? 0,
                Signal = macd?.Signal ?? 0,
                Histogram = macd?.Histogram ?? 0,
                Trend = (macd?.Histogram ?? 0) > 0 ? "BULLISH" : (macd?.Histogram ?? 0) < 0 ? "BEARISH" : "NEUTRAL"
            },
            ATR = new ATRData
            {
                Value = atr?.Value ?? 0,
                StopLossDistance = (atr?.Value ?? 0) * 2
            }
        };
    }
}

public class AnalyzeRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1h";
    public string? Model { get; set; }
}

public class ConsensusRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = "1h";
    public List<string>? Models { get; set; }
}

public class TestConnectionRequest
{
    public string? Model { get; set; }
}