using System.Text;
using System.Text.Json;
using CryptoTrading.API.Configuration;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.AI;

public class OllamaAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly AISettings _settings;
    private readonly ILogger<OllamaAIProvider> _logger;
    private readonly string _systemPrompt;

    public string ProviderName => "Ollama";

    public OllamaAIProvider(
        HttpClient httpClient,
        IOptions<AISettings> settings,
        ILogger<OllamaAIProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.OllamaBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _systemPrompt = BuildSystemPrompt();
    }

    private string BuildSystemPrompt()
    {
        return @"You are a crypto market analysis assistant.

You are NOT a financial advisor.

You do not guarantee profits.

You do not claim a guaranteed win rate.

You do not invent missing market information.

Analyze only the data supplied to you.

Evaluate:

1. Higher timeframe trend
2. Support and resistance
3. FVG
4. Order Block
5. Volume
6. RSI
7. Price action
8. Entry quality
9. Stop-loss placement
10. Take-profit placement
11. Risk/reward
12. Market condition

Possible conclusions:

BUY
SELL
WAIT

If evidence is conflicting or insufficient, return WAIT.

Prefer WAIT over a low-quality setup.

Return structured JSON.

Required response:

{
  ""decision"": ""BUY|SELL|WAIT"",
  ""confidence"": 0,
  ""marketCondition"": ""TRENDING|RANGING|VOLATILE|UNCLEAR"",
  ""reasons"": [],
  ""risks"": [],
  ""entryValid"": true,
  ""stopLossValid"": true,
  ""riskRewardValid"": true,
  ""summary"": """",
  ""entryPrice"": 0,
  ""stopLoss"": 0,
  ""takeProfit1"": 0,
  ""takeProfit2"": 0,
  ""takeProfit3"": 0,
  ""riskReward"": 0
}

Confidence means confidence in the quality of the analysis, NOT probability of profit.

Never claim 99% accuracy.";
    }

    public async Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisRequest request)
    {
        try
        {
            var prompt = BuildAnalysisPrompt(request);

            var ollamaRequest = new OllamaGenerateRequest
            {
                Model = request.Model,
                Prompt = prompt,
                System = _systemPrompt,
                Temperature = request.Temperature,
                Stream = false,
                Format = "json"
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending analysis request to Ollama model: {Model}", request.Model);

            var response = await _httpClient.PostAsync("/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return CreateErrorResult($"Ollama API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseContent);

            if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
            {
                _logger.LogWarning("Empty response from Ollama");
                return CreateErrorResult("Empty response from Ollama");
            }

            // Parse the JSON response from Ollama
            var analysisResult = JsonSerializer.Deserialize<AIAnalysisResult>(ollamaResponse.Response);

            if (analysisResult == null)
            {
                _logger.LogWarning("Failed to parse Ollama JSON response: {Response}", ollamaResponse.Response);
                return CreateErrorResult("Failed to parse AI response");
            }

            _logger.LogInformation("AI Analysis complete: {Decision} (Confidence: {Confidence})",
                analysisResult.Decision, analysisResult.Confidence);

            return analysisResult;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Ollama request timed out after {Timeout}s", _settings.TimeoutSeconds);
            return CreateErrorResult($"Request timed out after {_settings.TimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            return CreateErrorResult($"Error: {ex.Message}");
        }
    }

    private string BuildAnalysisPrompt(AIAnalysisRequest request)
    {
        var ctx = request.MarketContext;
        var sb = new StringBuilder();

        sb.AppendLine($"ANALYZE: {request.Symbol} on {request.Timeframe} timeframe");
        sb.AppendLine();

        sb.AppendLine("=== MARKET DATA ===");
        sb.AppendLine($"Current Price: ${ctx.CurrentPrice:N2}");
        sb.AppendLine($"24h Change: {ctx.Change24h:+#.##;-#.##;0}%");
        sb.AppendLine($"24h Volume: ${ctx.Volume24h:N0}");
        sb.AppendLine();

        sb.AppendLine("=== HIGHER TIMEFRAME BIAS ===");
        sb.AppendLine($"1H: {ctx.Bias1h.Direction} (Strength: {ctx.Bias1h.Strength}) - {string.Join(", ", ctx.Bias1h.Reasons)}");
        sb.AppendLine($"4H: {ctx.Bias4h.Direction} (Strength: {ctx.Bias4h.Strength}) - {string.Join(", ", ctx.Bias4h.Reasons)}");
        sb.AppendLine($"1D: {ctx.Bias1d.Direction} (Strength: {ctx.Bias1d.Strength}) - {string.Join(", ", ctx.Bias1d.Reasons)}");
        sb.AppendLine();

        sb.AppendLine("=== TECHNICAL INDICATORS ===");
        sb.AppendLine($"RSI: {ctx.Indicators.RSI.Value:F1} ({ctx.Indicators.RSI.Signal})");
        sb.AppendLine($"  Overbought: {ctx.Indicators.RSI.IsOverbought}, Oversold: {ctx.Indicators.RSI.IsOversold}");
        sb.AppendLine($"  Bullish Confirmation: {ctx.Indicators.RSI.BullishConfirmation}, Bearish: {ctx.Indicators.RSI.BearishConfirmation}");
        sb.AppendLine();
        sb.AppendLine($"Volume: Current={ctx.Indicators.Volume.CurrentVolume:N0}, Avg={ctx.Indicators.Volume.AverageVolume:N0}, Ratio={ctx.Indicators.Volume.VolumeRatio:F2}x");
        sb.AppendLine($"  Volume Spike: {ctx.Indicators.Volume.VolumeSpike}");
        sb.AppendLine($"  Bullish: {ctx.Indicators.Volume.BullishConfirmation}, Bearish: {ctx.Indicators.Volume.BearishConfirmation}");
        sb.AppendLine();
        sb.AppendLine($"EMA: 9={ctx.Indicators.EMA.EMA9:N2}, 21={ctx.Indicators.EMA.EMA21:N2}, 50={ctx.Indicators.EMA.EMA50:N2}");
        sb.AppendLine($"  Trend: {ctx.Indicators.EMA.Trend}, Bullish Alignment: {ctx.Indicators.EMA.BullishAlignment}, Bearish: {ctx.Indicators.EMA.BearishAlignment}");
        sb.AppendLine();
        sb.AppendLine($"MACD: MACD={ctx.Indicators.MACD.MACD:N4}, Signal={ctx.Indicators.MACD.Signal:N4}, Histogram={ctx.Indicators.MACD.Histogram:N4}");
        sb.AppendLine($"  Trend: {ctx.Indicators.MACD.Trend}");
        sb.AppendLine();
        sb.AppendLine($"ATR: {ctx.Indicators.ATR.Value:N2}, SL Distance: {ctx.Indicators.ATR.StopLossDistance:N2}");
        sb.AppendLine();

        sb.AppendLine("=== SUPPORT / RESISTANCE ===");
        foreach (var level in ctx.SupportResistance.Take(10))
        {
            sb.AppendLine($"{level.Type} @ ${level.Price:N2} (Touches: {level.Touches}, TF: {level.Timeframe}, Strength: {level.Strength:F1})");
        }
        sb.AppendLine();

        sb.AppendLine("=== FAIR VALUE GAPS (FVG) ===");
        foreach (var fvg in ctx.FVGs.Take(5))
        {
            var status = fvg.Mitigated ? "MITIGATED" : "ACTIVE";
            sb.AppendLine($"{fvg.Direction} FVG: ${fvg.Bottom:N2} - ${fvg.Top:N2} [{status}] (Created: {fvg.CreatedAt:yyyy-MM-dd HH:mm})");
        }
        sb.AppendLine();

        sb.AppendLine("=== ORDER BLOCKS ===");
        foreach (var ob in ctx.OrderBlocks.Take(5))
        {
            var status = ob.Mitigated ? "MITIGATED" : "ACTIVE";
            sb.AppendLine($"{ob.Direction} OB: ${ob.ZoneBottom:N2} - ${ob.ZoneTop:N2} [{status}] (Strength: {ob.Strength}, Created: {ob.CreatedAt:yyyy-MM-dd HH:mm})");
        }
        sb.AppendLine();

        sb.AppendLine("=== RECENT PRICE ACTION (last 20 candles) ===");
        foreach (var candle in ctx.RecentCandles.TakeLast(20))
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(candle.Timestamp).ToString("MM-dd HH:mm");
            var direction = candle.Close >= candle.Open ? "🟢" : "🔴";
            sb.AppendLine($"{date} {direction} O:{candle.Open:N2} H:{candle.High:N2} L:{candle.Low:N2} C:{candle.Close:N2} V:{candle.Volume:N0}");
        }
        sb.AppendLine();

        sb.AppendLine("Provide your analysis in the required JSON format.");

        return sb.ToString();
    }

    private AIAnalysisResult CreateErrorResult(string error)
    {
        return new AIAnalysisResult
        {
            Decision = "WAIT",
            Confidence = 0,
            MarketCondition = "UNCLEAR",
            Reasons = new[] { $"Analysis error: {error}" },
            Risks = new[] { "AI analysis unavailable" },
            EntryValid = false,
            StopLossValid = false,
            RiskRewardValid = false,
            Summary = $"AI analysis failed: {error}"
        };
    }

    public async Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Ollama models: {StatusCode}", response.StatusCode);
                return Array.Empty<string>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(content);

            return modelsResponse?.Models.Select(m => m.Name).ToArray() ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ollama models");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}