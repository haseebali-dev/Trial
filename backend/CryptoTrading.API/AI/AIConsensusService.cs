using CryptoTrading.API.Configuration;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.AI;

public interface IAIConsensusService
{
    Task<AIConsensusResult> GetConsensusAsync(string symbol, string timeframe, IEnumerable<string> modelNames, MarketContext context);
    Task<AIAnalysisResult> AnalyzeWithSingleModelAsync(string symbol, string timeframe, string modelName, MarketContext context);
}

public class AIConsensusService : IAIConsensusService
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly IOptions<AISettings> _settings;
    private readonly ILogger<AIConsensusService> _logger;

    public AIConsensusService(
        IEnumerable<IAIProvider> providers,
        IOptions<AISettings> settings,
        ILogger<AIConsensusService> logger)
    {
        _providers = providers;
        _settings = settings;
        _logger = logger;
    }

    public async Task<AIConsensusResult> GetConsensusAsync(string symbol, string timeframe, IEnumerable<string> modelNames, MarketContext context)
    {
        var modelsToUse = modelNames.Take(_settings.Value.MaxModels).ToList();

        if (!modelsToUse.Any())
        {
            modelsToUse.Add(_settings.Value.DefaultModel);
        }

        _logger.LogInformation("Getting AI consensus for {Symbol} using {Count} models: {Models}",
            symbol, modelsToUse.Count, string.Join(", ", modelsToUse));

        var tasks = modelsToUse.Select(model => AnalyzeWithModelAsync(model, symbol, timeframe, context));
        var results = await Task.WhenAll(tasks);

        var validResults = results.Where(r => r != null && r.Decision != "ERROR").ToList();

        if (!validResults.Any())
        {
            return new AIConsensusResult
            {
                Symbol = symbol,
                Timeframe = timeframe,
                ConsensusDecision = "WAIT",
                AgreeCount = 0,
                TotalModels = modelsToUse.Count,
                ModelDecisions = results.Select(r => new ModelDecision
                {
                    Model = modelsToUse.ElementAtOrDefault(results.ToList().IndexOf(r ?? results.First())) ?? "unknown",
                    Decision = r?.Decision ?? "ERROR",
                    Confidence = r?.Confidence ?? 0
                }).ToList(),
                Timestamp = DateTime.UtcNow
            };
        }

        // Count decisions
        var buyCount = validResults.Count(r => r.Decision == "BUY");
        var sellCount = validResults.Count(r => r.Decision == "SELL");
        var waitCount = validResults.Count(r => r.Decision == "WAIT");

        // Determine consensus
        string consensusDecision;
        if (buyCount > sellCount && buyCount > waitCount)
        {
            consensusDecision = "BUY";
        }
        else if (sellCount > buyCount && sellCount > waitCount)
        {
            consensusDecision = "SELL";
        }
        else
        {
            consensusDecision = "WAIT";
        }

        var agreeCount = consensusDecision switch
        {
            "BUY" => buyCount,
            "SELL" => sellCount,
            _ => waitCount
        };

        var consensus = new AIConsensusResult
        {
            Symbol = symbol,
            Timeframe = timeframe,
            ConsensusDecision = consensusDecision,
            AgreeCount = agreeCount,
            TotalModels = modelsToUse.Count,
            ModelDecisions = modelsToUse.Zip(results, (model, result) => new ModelDecision
            {
                Model = model,
                Decision = result?.Decision ?? "ERROR",
                Confidence = result?.Confidence ?? 0,
                Summary = result?.Summary ?? ""
            }).ToList(),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("AI Consensus for {Symbol}: {Decision} ({Agree}/{Total} models)",
            symbol, consensusDecision, agreeCount, modelsToUse.Count);

        return consensus;
    }

    public async Task<AIAnalysisResult> AnalyzeWithSingleModelAsync(string symbol, string timeframe, string modelName, MarketContext context)
    {
        var provider = _providers.FirstOrDefault();

        if (provider == null)
        {
            _logger.LogError("No AI provider available");
            return CreateErrorResult("No AI provider configured");
        }

        var request = new AIAnalysisRequest
        {
            Symbol = symbol,
            Timeframe = timeframe,
            MarketContext = context,
            Model = modelName,
            Temperature = _settings.Value.Temperature
        };

        return await provider.AnalyzeAsync(request);
    }

    private async Task<AIAnalysisResult?> AnalyzeWithModelAsync(string model, string symbol, string timeframe, MarketContext context)
    {
        var provider = _providers.FirstOrDefault();

        if (provider == null)
        {
            return null;
        }

        var request = new AIAnalysisRequest
        {
            Symbol = symbol,
            Timeframe = timeframe,
            MarketContext = context,
            Model = model,
            Temperature = _settings.Value.Temperature
        };

        try
        {
            return await provider.AnalyzeAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing with model {Model}", model);
            return CreateErrorResult($"Model {model} error: {ex.Message}");
        }
    }

    private AIAnalysisResult CreateErrorResult(string error)
    {
        return new AIAnalysisResult
        {
            Decision = "ERROR",
            Confidence = 0,
            MarketCondition = "UNCLEAR",
            Reasons = new[] { error },
            Risks = new[] { "AI analysis failed" },
            EntryValid = false,
            StopLossValid = false,
            RiskRewardValid = false,
            Summary = error
        };
    }
}

public class AIConsensusResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string ConsensusDecision { get; set; } = "WAIT";
    public int AgreeCount { get; set; }
    public int TotalModels { get; set; }
    public List<ModelDecision> ModelDecisions { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ModelDecision
{
    public string Model { get; set; } = string.Empty;
    public string Decision { get; set; } = "WAIT";
    public int Confidence { get; set; }
    public string Summary { get; set; } = string.Empty;
}