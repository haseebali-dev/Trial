using CryptoTrading.API.Configuration;
using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoTrading.API.Services;

public class ScannerService : IScannerService
{
    private readonly IMarketDataService _marketDataService;
    private readonly ISignalGenerationService _signalGenerationService;
    private readonly ISmartMoneyConceptsService _smcService;
    private readonly IPatternRecognitionService _patternRecognitionService;
    private readonly ITechnicalAnalysisService _technicalAnalysisService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ScannerService> _logger;
    private readonly ScannerSettings _settings;
    private readonly SemaphoreSlim _scanSemaphore;
    private bool _isScanning = false;

    public ScannerService(
        IMarketDataService marketDataService,
        ISignalGenerationService signalGenerationService,
        ISmartMoneyConceptsService smcService,
        IPatternRecognitionService patternRecognitionService,
        ITechnicalAnalysisService technicalAnalysisService,
        IMemoryCache cache,
        ILogger<ScannerService> logger,
        IOptions<ScannerSettings> settings)
    {
        _marketDataService = marketDataService;
        _signalGenerationService = signalGenerationService;
        _smcService = smcService;
        _patternRecognitionService = patternRecognitionService;
        _technicalAnalysisService = technicalAnalysisService;
        _cache = cache;
        _logger = logger;
        _settings = settings.Value;
        _scanSemaphore = new SemaphoreSlim(_settings.MaxConcurrentScans);
    }

    public async Task<ScannerSummaryDto> ScanMarketAsync(ScanRequestDto request)
    {
        if (_isScanning)
        {
            _logger.LogWarning("Scanner is already running, skipping new scan request");
            return await GetCachedScanAsync() ?? CreateEmptySummary();
        }

        _isScanning = true;
        var scanStartTime = DateTime.UtcNow;
        var symbols = request.Symbols?.Any() == true ? request.Symbols : _settings.DefaultSymbols;
        var timeframes = request.Timeframes?.Any() == true ? request.Timeframes : new List<string> { "1h", "4h" };
        var minimumScore = request.MinimumScore > 0 ? request.MinimumScore : _settings.MinimumScore;
        var maxConcurrent = request.MaxConcurrentScans > 0 ? request.MaxConcurrentScans : _settings.MaxConcurrentScans;

        _logger.LogInformation("Starting market scan for {SymbolCount} symbols across {TimeframeCount} timeframes",
            symbols.Count, timeframes.Count);

        var allResults = new List<ScanResultDto>();
        var scanTasks = new List<Task>();

        foreach (var timeframe in timeframes)
        {
            // Process symbols in batches to control concurrency
            var batches = symbols.Chunk(maxConcurrent).ToList();

            foreach (var batch in batches)
            {
                var batchTasks = batch.Select(async symbol =>
                {
                    await _scanSemaphore.WaitAsync();
                    try
                    {
                        var result = await ScanSingleSymbolAsync(symbol, timeframe);
                        if (result != null && result.OverallScore >= minimumScore)
                        {
                            lock (allResults)
                            {
                                allResults.Add(result);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning {Symbol} on {Timeframe}", symbol, timeframe);
                    }
                    finally
                    {
                        _scanSemaphore.Release();
                    }
                });

                scanTasks.AddRange(batchTasks);
            }
        }

        await Task.WhenAll(scanTasks);

        // Sort by score
        var sortedResults = allResults
            .OrderByDescending(r => r.OverallScore)
            .ToList();

        var bullishCount = sortedResults.Count(r => r.OverallDirection == "BULLISH");
        var bearishCount = sortedResults.Count(r => r.OverallDirection == "BEARISH");
        var neutralCount = sortedResults.Count(r => r.OverallDirection == "NEUTRAL");

        var summary = new ScannerSummaryDto(
            TotalSymbolsScanned: symbols.Count * timeframes.Count,
            BullishSymbols: bullishCount,
            BearishSymbols: bearishCount,
            NeutralSymbols: neutralCount,
            TopOpportunities: sortedResults.Where(r => r.OverallDirection == "BULLISH").Take(10).ToList(),
            TopRisks: sortedResults.Where(r => r.OverallDirection == "BEARISH").Take(10).ToList(),
            ScanTimestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ScanDuration: DateTime.UtcNow - scanStartTime
        );

        // Cache the results
        _cache.Set("scanner:last_scan", summary, TimeSpan.FromMinutes(_settings.ScanIntervalMinutes));
        _isScanning = false;

        _logger.LogInformation("Market scan completed in {Duration}ms. Found {Bullish} bullish, {Bearish} bearish, {Neutral} neutral",
            summary.ScanDuration.TotalMilliseconds, bullishCount, bearishCount, neutralCount);

        return summary;
    }

    public async Task<SymbolScanDetailsDto> ScanSymbolAsync(string symbol, string timeframe)
    {
        _logger.LogDebug("Performing detailed scan for {Symbol} on {Timeframe}", symbol, timeframe);

        var scanResult = await ScanSingleSymbolAsync(symbol, timeframe);
        var signals = await _signalGenerationService.GetSignalsAsync(symbol, timeframe);
        var smcAnalysis = await _smcService.GetSmcAnalysisAsync(symbol, timeframe);
        var patterns = await _patternRecognitionService.GetPatternsAsync(symbol, timeframe);
        var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe);
        var ticker = await _marketDataService.GetTickerAsync(symbol);

        return new SymbolScanDetailsDto(
            Symbol: symbol,
            Timeframe: timeframe,
            ScanResult: scanResult!,
            Signals: signals,
            SmcAnalysis: smcAnalysis,
            Patterns: patterns,
            Indicators: indicators,
            Ticker: ticker
        );
    }

    public async Task<IReadOnlyList<ScanResultDto>> ScanSymbolsAsync(List<string> symbols, string timeframe, decimal minimumScore = 5)
    {
        var results = new List<ScanResultDto>();

        foreach (var symbol in symbols)
        {
            try
            {
                var result = await ScanSingleSymbolAsync(symbol, timeframe);
                if (result != null && result.OverallScore >= minimumScore)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning {Symbol}", symbol);
            }
        }

        return results.OrderByDescending(r => r.OverallScore).ToList();
    }

    public Task<bool> IsScanningAsync()
    {
        return Task.FromResult(_isScanning);
    }

    private async Task<ScanResultDto?> ScanSingleSymbolAsync(string symbol, string timeframe)
    {
        try
        {
            // Get all analysis components in parallel
            var signalsTask = _signalGenerationService.GetSignalsAsync(symbol, timeframe);
            var smcTask = _smcService.GetSmcAnalysisAsync(symbol, timeframe);
            var patternsTask = _patternRecognitionService.GetPatternsAsync(symbol, timeframe);
            var tickerTask = _marketDataService.GetTickerAsync(symbol);

            await Task.WhenAll(signalsTask, smcTask, patternsTask, tickerTask);

            var signals = signalsTask.Result;
            var smcAnalysis = smcTask.Result;
            var patterns = patternsTask.Result;
            var ticker = tickerTask.Result;

            // Calculate SMC score
            var smcScore = CalculateSmcScore(smcAnalysis);

            // Calculate Pattern score
            var patternScore = CalculatePatternScore(patterns);

            // Calculate overall score combining all strategies
            var overallScore = CalculateOverallScore(signals, smcScore, patternScore);
            var overallDirection = overallScore > 60 ? "BULLISH" : overallScore < 40 ? "BEARISH" : "NEUTRAL";

            // Find top signal
            var topSignal = signals.Signals.OrderByDescending(s => s.Confidence).FirstOrDefault();

            return new ScanResultDto(
                Symbol: symbol,
                Timeframe: timeframe,
                OverallScore: Math.Round(overallScore, 2),
                OverallDirection: overallDirection,
                SignalCount: signals.Signals.Count,
                TrendFollowingScore: Math.Round(signals.StrategyScores.GetValueOrDefault("TrendFollowing", 0), 2),
                MeanReversionScore: Math.Round(signals.StrategyScores.GetValueOrDefault("MeanReversion", 0), 2),
                BreakoutScore: Math.Round(signals.StrategyScores.GetValueOrDefault("Breakout", 0), 2),
                MomentumScore: Math.Round(signals.StrategyScores.GetValueOrDefault("Momentum", 0), 2),
                SmcScore: Math.Round(smcScore, 2),
                PatternScore: Math.Round(patternScore, 2),
                TopSignalType: topSignal?.Type ?? "NONE",
                TopSignalDirection: topSignal?.Direction ?? "NONE",
                TopSignalConfidence: topSignal?.Confidence ?? 0,
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning {Symbol} on {Timeframe}", symbol, timeframe);
            return null;
        }
    }

    private decimal CalculateOverallScore(SignalSummaryDto signals, decimal smcScore, decimal patternScore)
    {
        // Weighted combination: 50% strategy signals, 30% SMC, 20% patterns
        var strategyWeight = 0.5m;
        var smcWeight = 0.3m;
        var patternWeight = 0.2m;

        var strategyScore = signals.OverallScore;

        return (strategyScore * strategyWeight) + (smcScore * smcWeight) + (patternScore * patternWeight);
    }

    private decimal CalculateSmcScore(SmcAnalysisResponse smcAnalysis)
    {
        var score = 50m; // Neutral base

        // Order Blocks
        var bullishOBs = smcAnalysis.OrderBlocks.Where(ob => ob.Type == OrderBlockType.Bullish && !ob.IsMitigated).ToList();
        var bearishOBs = smcAnalysis.OrderBlocks.Where(ob => ob.Type == OrderBlockType.Bearish && !ob.IsMitigated).ToList();

        if (bullishOBs.Any())
        {
            var strongestBullOB = bullishOBs.MaxBy(ob => ob.Strength);
            if (strongestBullOB != null && smcAnalysis.CurrentPrice <= strongestBullOB.HighPrice * 1.02m)
            {
                score += Math.Min(strongestBullOB.Strength * 0.3m, 25m);
            }
        }

        if (bearishOBs.Any())
        {
            var strongestBearOB = bearishOBs.MaxBy(ob => ob.Strength);
            if (strongestBearOB != null && smcAnalysis.CurrentPrice >= strongestBearOB.LowPrice * 0.98m)
            {
                score -= Math.Min(strongestBearOB.Strength * 0.3m, 25m);
            }
        }

        // Fair Value Gaps
        var bullishFVGs = smcAnalysis.FairValueGaps.Where(fvg => fvg.Type == OrderBlockType.Bullish && !fvg.IsFilled).ToList();
        var bearishFVGs = smcAnalysis.FairValueGaps.Where(fvg => fvg.Type == OrderBlockType.Bearish && !fvg.IsFilled).ToList();

        if (bullishFVGs.Any())
        {
            score += Math.Min(bullishFVGs.Count * 5m, 15m);
        }

        if (bearishFVGs.Any())
        {
            score -= Math.Min(bearishFVGs.Count * 5m, 15m);
        }

        // Structure Breaks
        var recentBreak = smcAnalysis.StructureBreaks.FirstOrDefault();
        if (recentBreak != null)
        {
            if (recentBreak.Type == StructureBreakType.CHoCH)
            {
                if (recentBreak.NewStructure == MarketStructure.Bullish)
                    score += 20m;
                else
                    score -= 20m;
            }
            else if (recentBreak.Type == StructureBreakType.BOS)
            {
                if (recentBreak.NewStructure == MarketStructure.Bullish)
                    score += 10m;
                else
                    score -= 10m;
            }
        }

        // Premium/Discount Zone
        if (smcAnalysis.PremiumDiscountZone != null)
        {
            if (smcAnalysis.PremiumDiscountZone.Type == ZoneType.Discount)
                score += 10m;
            else
                score -= 10m;
        }

        return Math.Clamp(score, 0m, 100m);
    }

    private decimal CalculatePatternScore(PatternRecognitionResponse patterns)
    {
        var score = 50m; // Neutral base

        // Candle patterns
        var bullishCandles = patterns.CandlePatterns.Where(p => p.Direction == PatternDirection.Bullish && p.Confidence >= 70).ToList();
        var bearishCandles = patterns.CandlePatterns.Where(p => p.Direction == PatternDirection.Bearish && p.Confidence >= 70).ToList();

        score += bullishCandles.Sum(p => Math.Min(p.Confidence * 0.1m, 5m));
        score -= bearishCandles.Sum(p => Math.Min(p.Confidence * 0.1m, 5m));

        // Chart patterns (higher weight)
        var bullishCharts = patterns.ChartPatterns.Where(p => p.Direction == PatternDirection.Bullish && p.Confidence >= 70).ToList();
        var bearishCharts = patterns.ChartPatterns.Where(p => p.Direction == PatternDirection.Bearish && p.Confidence >= 70).ToList();

        score += bullishCharts.Sum(p => Math.Min(p.Confidence * 0.15m, 10m));
        score -= bearishCharts.Sum(p => Math.Min(p.Confidence * 0.15m, 10m));

        return Math.Clamp(score, 0m, 100m);
    }

    private async Task<ScannerSummaryDto?> GetCachedScanAsync()
    {
        return _cache.TryGetValue("scanner:last_scan", out ScannerSummaryDto? cached) ? cached : null;
    }

    private ScannerSummaryDto CreateEmptySummary()
    {
        return new ScannerSummaryDto(
            TotalSymbolsScanned: 0,
            BullishSymbols: 0,
            BearishSymbols: 0,
            NeutralSymbols: 0,
            TopOpportunities: [],
            TopRisks: [],
            ScanTimestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ScanDuration: TimeSpan.Zero
        );
    }
}