using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.API.Services;

public class SignalGenerationService : ISignalGenerationService
{
    private readonly ITechnicalAnalysisService _technicalAnalysisService;
    private readonly IMarketDataService _marketDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SignalGenerationService> _logger;

    public SignalGenerationService(
        ITechnicalAnalysisService technicalAnalysisService,
        IMarketDataService marketDataService,
        IMemoryCache cache,
        ILogger<SignalGenerationService> logger)
    {
        _technicalAnalysisService = technicalAnalysisService;
        _marketDataService = marketDataService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SignalSummaryDto> GetSignalsAsync(string symbol, string timeframe, string[]? strategies = null)
    {
        var cacheKey = $"signals:{symbol}:{timeframe}";
        if (_cache.TryGetValue(cacheKey, out SignalSummaryDto? cached))
        {
            return cached!;
        }

        var allSignals = new List<SignalDto>();
        var strategyScores = new Dictionary<string, decimal>();

        var trendSignals = await GetTrendFollowingSignalsAsync(symbol, timeframe);
        var meanReversionSignals = await GetMeanReversionSignalsAsync(symbol, timeframe);
        var breakoutSignals = await GetBreakoutSignalsAsync(symbol, timeframe);
        var momentumSignals = await GetMomentumSignalsAsync(symbol, timeframe);

        allSignals.AddRange(trendSignals);
        allSignals.AddRange(meanReversionSignals);
        allSignals.AddRange(breakoutSignals);
        allSignals.AddRange(momentumSignals);

        // Calculate strategy scores
        strategyScores["TrendFollowing"] = CalculateStrategyScore(trendSignals);
        strategyScores["MeanReversion"] = CalculateStrategyScore(meanReversionSignals);
        strategyScores["Breakout"] = CalculateStrategyScore(breakoutSignals);
        strategyScores["Momentum"] = CalculateStrategyScore(momentumSignals);

        // Calculate overall score and direction
        var bullishScore = allSignals.Where(s => s.Direction == "LONG").Sum(s => s.Confidence);
        var bearishScore = allSignals.Where(s => s.Direction == "SHORT").Sum(s => s.Confidence);
        var totalScore = bullishScore + bearishScore;

        var overallScore = totalScore == 0 ? 50m : (bullishScore / totalScore) * 100;
        var overallDirection = overallScore > 60 ? "BULLISH" : overallScore < 40 ? "BEARISH" : "NEUTRAL";

        var summary = new SignalSummaryDto(
            Symbol: symbol,
            Timeframe: timeframe,
            OverallScore: overallScore,
            OverallDirection: overallDirection,
            Signals: allSignals,
            StrategyScores: strategyScores,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        _cache.Set(cacheKey, summary, TimeSpan.FromSeconds(30));
        return summary;
    }

    public async Task<IReadOnlyList<SignalDto>> GetTrendFollowingSignalsAsync(string symbol, string timeframe)
    {
        var signals = new List<SignalDto>();
        var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, 200);
        var ticker = await _marketDataService.GetTickerAsync(symbol);

        if (indicators.Ema9.Count == 0 || indicators.Ema21.Count == 0 || indicators.Ema50.Count == 0)
            return signals;

        var currentPrice = ticker.Price;
        var ema9 = indicators.Ema9.Last().Value;
        var ema21 = indicators.Ema21.Last().Value;
        var ema50 = indicators.Ema50.Last().Value;

        // EMA 9/21 Crossover
        if (indicators.Ema9.Count >= 2 && indicators.Ema21.Count >= 2)
        {
            var prevEma9 = indicators.Ema9[^2].Value;
            var prevEma21 = indicators.Ema21[^2].Value;

            if (prevEma9 <= prevEma21 && ema9 > ema21 && currentPrice > ema50)
            {
                var confidence = CalculateTrendConfidence(ema9, ema21, ema50, currentPrice, true);
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "ENTRY",
                    Direction: "LONG",
                    Confidence: confidence,
                    EntryPrice: currentPrice,
                    StopLoss: ema50 * 0.98m,
                    TakeProfit: currentPrice + (currentPrice - ema50) * 2,
                    Strategy: "TrendFollowing",
                    Reason: "EMA 9/21 bullish crossover with price above EMA 50",
                    Metadata: new Dictionary<string, object>
                    {
                        ["ema9"] = ema9,
                        ["ema21"] = ema21,
                        ["ema50"] = ema50
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
            else if (prevEma9 >= prevEma21 && ema9 < ema21 && currentPrice < ema50)
            {
                var confidence = CalculateTrendConfidence(ema9, ema21, ema50, currentPrice, false);
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "ENTRY",
                    Direction: "SHORT",
                    Confidence: confidence,
                    EntryPrice: currentPrice,
                    StopLoss: ema50 * 1.02m,
                    TakeProfit: currentPrice - (ema50 - currentPrice) * 2,
                    Strategy: "TrendFollowing",
                    Reason: "EMA 9/21 bearish crossover with price below EMA 50",
                    Metadata: new Dictionary<string, object>
                    {
                        ["ema9"] = ema9,
                        ["ema21"] = ema21,
                        ["ema50"] = ema50
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        // ADX Trend Strength
        if (indicators.Adx14.Count > 0)
        {
            var adx = indicators.Adx14.Last();
            if (adx.Adx > 25 && adx.PlusDi > adx.MinusDi && currentPrice > ema21)
            {
                var confidence = Math.Min(adx.Adx / 50m, 1m) * 75m;
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "CONFIRMATION",
                    Direction: "LONG",
                    Confidence: confidence,
                    EntryPrice: currentPrice,
                    StopLoss: currentPrice * 0.97m,
                    TakeProfit: null,
                    Strategy: "TrendFollowing",
                    Reason: $"Strong uptrend detected (ADX: {adx.Adx:F1})",
                    Metadata: new Dictionary<string, object>
                    {
                        ["adx"] = adx.Adx,
                        ["plusDi"] = adx.PlusDi,
                        ["minusDi"] = adx.MinusDi
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
            else if (adx.Adx > 25 && adx.MinusDi > adx.PlusDi && currentPrice < ema21)
            {
                var confidence = Math.Min(adx.Adx / 50m, 1m) * 75m;
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "CONFIRMATION",
                    Direction: "SHORT",
                    Confidence: confidence,
                    EntryPrice: currentPrice,
                    StopLoss: currentPrice * 1.03m,
                    TakeProfit: null,
                    Strategy: "TrendFollowing",
                    Reason: $"Strong downtrend detected (ADX: {adx.Adx:F1})",
                    Metadata: new Dictionary<string, object>
                    {
                        ["adx"] = adx.Adx,
                        ["plusDi"] = adx.PlusDi,
                        ["minusDi"] = adx.MinusDi
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        return signals;
    }

    public async Task<IReadOnlyList<SignalDto>> GetMeanReversionSignalsAsync(string symbol, string timeframe)
    {
        var signals = new List<SignalDto>();
        var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, 200);
        var ticker = await _marketDataService.GetTickerAsync(symbol);

        if (indicators.Rsi14.Count == 0 || indicators.BollingerBands20.Count == 0)
            return signals;

        var currentPrice = ticker.Price;
        var rsi = indicators.Rsi14.Last().Value;
        var bb = indicators.BollingerBands20.Last();

        // RSI Oversold + BB Lower Band
        if (rsi < 30 && bb.PercentB < 0.2m)
        {
            var confidence = (30 - rsi) / 30m * 50m + (1 - bb.PercentB) * 50m;
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "LONG",
                Confidence: Math.Min(confidence, 95m),
                EntryPrice: currentPrice,
                StopLoss: bb.Lower * 0.98m,
                TakeProfit: bb.Middle,
                Strategy: "MeanReversion",
                Reason: $"Oversold conditions (RSI: {rsi:F1}, BB %B: {bb.PercentB:F2})",
                Metadata: new Dictionary<string, object>
                {
                    ["rsi"] = rsi,
                    ["bbPercentB"] = bb.PercentB,
                    ["bbLower"] = bb.Lower,
                    ["bbMiddle"] = bb.Middle
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // RSI Overbought + BB Upper Band
        if (rsi > 70 && bb.PercentB > 0.8m)
        {
            var confidence = (rsi - 70) / 30m * 50m + (bb.PercentB - 0.5m) * 100m;
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "SHORT",
                Confidence: Math.Min(confidence, 95m),
                EntryPrice: currentPrice,
                StopLoss: bb.Upper * 1.02m,
                TakeProfit: bb.Middle,
                Strategy: "MeanReversion",
                Reason: $"Overbought conditions (RSI: {rsi:F1}, BB %B: {bb.PercentB:F2})",
                Metadata: new Dictionary<string, object>
                {
                    ["rsi"] = rsi,
                    ["bbPercentB"] = bb.PercentB,
                    ["bbUpper"] = bb.Upper,
                    ["bbMiddle"] = bb.Middle
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // RSI Divergence detection (simplified)
        if (indicators.Rsi14.Count >= 20)
        {
            var recentRsi = indicators.Rsi14.TakeLast(20).ToList();
            var minRsi = recentRsi.Min(r => r.Value);
            var maxRsi = recentRsi.Max(r => r.Value);

            if (rsi < 40 && rsi > minRsi + 5 && currentPrice < bb.Middle)
            {
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "CONFIRMATION",
                    Direction: "LONG",
                    Confidence: 60m,
                    EntryPrice: currentPrice,
                    StopLoss: null,
                    TakeProfit: null,
                    Strategy: "MeanReversion",
                    Reason: "Potential bullish divergence detected",
                    Metadata: new Dictionary<string, object> { ["rsi"] = rsi },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        return signals;
    }

    public async Task<IReadOnlyList<SignalDto>> GetBreakoutSignalsAsync(string symbol, string timeframe)
    {
        var signals = new List<SignalDto>();
        var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, 100);
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, 100);
        var ticker = await _marketDataService.GetTickerAsync(symbol);

        if (candles.Count < 20 || indicators.Atr14.Count == 0)
            return signals;

        var currentPrice = ticker.Price;
        var atr = indicators.Atr14.Last().Value;
        var recentCandles = candles.TakeLast(20).ToList();
        var high20 = recentCandles.Max(c => c.High);
        var low20 = recentCandles.Min(c => c.Low);

        // Breakout above recent high with volume
        if (currentPrice > high20 * 1.001m)
        {
            var volumeRatio = ticker.Volume24h / recentCandles.Average(c => c.Volume);
            var confidence = Math.Min(volumeRatio / 2m, 1m) * 80m;

            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "LONG",
                Confidence: confidence,
                EntryPrice: currentPrice,
                StopLoss: high20 * 0.99m,
                TakeProfit: currentPrice + (atr * 3),
                Strategy: "Breakout",
                Reason: $"Breakout above 20-period high with {volumeRatio:F1}x volume",
                Metadata: new Dictionary<string, object>
                {
                    ["high20"] = high20,
                    ["volumeRatio"] = volumeRatio,
                    ["atr"] = atr
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // Breakdown below recent low with volume
        if (currentPrice < low20 * 0.999m)
        {
            var volumeRatio = ticker.Volume24h / recentCandles.Average(c => c.Volume);
            var confidence = Math.Min(volumeRatio / 2m, 1m) * 80m;

            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "SHORT",
                Confidence: confidence,
                EntryPrice: currentPrice,
                StopLoss: low20 * 1.01m,
                TakeProfit: currentPrice - (atr * 3),
                Strategy: "Breakout",
                Reason: $"Breakdown below 20-period low with {volumeRatio:F1}x volume",
                Metadata: new Dictionary<string, object>
                {
                    ["low20"] = low20,
                    ["volumeRatio"] = volumeRatio,
                    ["atr"] = atr
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // Bollinger Band squeeze breakout
        if (indicators.BollingerBands20.Count >= 2)
        {
            var currentBB = indicators.BollingerBands20.Last();
            var prevBB = indicators.BollingerBands20[^2];

            if (currentBB.Bandwidth < 0.02m && currentBB.Bandwidth < prevBB.Bandwidth)
            {
                if (currentPrice > currentBB.Upper)
                {
                    signals.Add(new SignalDto(
                        Symbol: symbol,
                        Timeframe: timeframe,
                        Type: "ENTRY",
                        Direction: "LONG",
                        Confidence: 70m,
                        EntryPrice: currentPrice,
                        StopLoss: currentBB.Middle,
                        TakeProfit: currentPrice + (currentPrice - currentBB.Middle) * 2,
                        Strategy: "Breakout",
                        Reason: "Bollinger Band squeeze breakout to upside",
                        Metadata: new Dictionary<string, object>
                        {
                            ["bandwidth"] = currentBB.Bandwidth
                        },
                        Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    ));
                }
                else if (currentPrice < currentBB.Lower)
                {
                    signals.Add(new SignalDto(
                        Symbol: symbol,
                        Timeframe: timeframe,
                        Type: "ENTRY",
                        Direction: "SHORT",
                        Confidence: 70m,
                        EntryPrice: currentPrice,
                        StopLoss: currentBB.Middle,
                        TakeProfit: currentPrice - (currentBB.Middle - currentPrice) * 2,
                        Strategy: "Breakout",
                        Reason: "Bollinger Band squeeze breakout to downside",
                        Metadata: new Dictionary<string, object>
                        {
                            ["bandwidth"] = currentBB.Bandwidth
                        },
                        Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    ));
                }
            }
        }

        return signals;
    }

    public async Task<IReadOnlyList<SignalDto>> GetMomentumSignalsAsync(string symbol, string timeframe)
    {
        var signals = new List<SignalDto>();
        var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, 100);
        var ticker = await _marketDataService.GetTickerAsync(symbol);

        if (indicators.Macd.Count < 2 || indicators.Stochastic14.Count == 0)
            return signals;

        var currentPrice = ticker.Price;
        var currentMacd = indicators.Macd.Last();
        var prevMacd = indicators.Macd[^2];
        var stoch = indicators.Stochastic14.Last();

        // MACD Crossover
        if (prevMacd.Macd <= prevMacd.Signal && currentMacd.Macd > currentMacd.Signal && currentMacd.Histogram > 0)
        {
            var confidence = Math.Min(Math.Abs(currentMacd.Histogram) / (currentPrice * 0.001m), 1m) * 85m;
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "LONG",
                Confidence: confidence,
                EntryPrice: currentPrice,
                StopLoss: currentPrice * 0.97m,
                TakeProfit: currentPrice * 1.05m,
                Strategy: "Momentum",
                Reason: "MACD bullish crossover",
                Metadata: new Dictionary<string, object>
                {
                    ["macd"] = currentMacd.Macd,
                    ["signal"] = currentMacd.Signal,
                    ["histogram"] = currentMacd.Histogram
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }
        else if (prevMacd.Macd >= prevMacd.Signal && currentMacd.Macd < currentMacd.Signal && currentMacd.Histogram < 0)
        {
            var confidence = Math.Min(Math.Abs(currentMacd.Histogram) / (currentPrice * 0.001m), 1m) * 85m;
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "ENTRY",
                Direction: "SHORT",
                Confidence: confidence,
                EntryPrice: currentPrice,
                StopLoss: currentPrice * 1.03m,
                TakeProfit: currentPrice * 0.95m,
                Strategy: "Momentum",
                Reason: "MACD bearish crossover",
                Metadata: new Dictionary<string, object>
                {
                    ["macd"] = currentMacd.Macd,
                    ["signal"] = currentMacd.Signal,
                    ["histogram"] = currentMacd.Histogram
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // Stochastic Oversold/Overbought
        if (stoch.K < 20 && stoch.D < 20 && stoch.K > stoch.D)
        {
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "CONFIRMATION",
                Direction: "LONG",
                Confidence: 70m,
                EntryPrice: currentPrice,
                StopLoss: null,
                TakeProfit: null,
                Strategy: "Momentum",
                Reason: $"Stochastic oversold reversal (K: {stoch.K:F1}, D: {stoch.D:F1})",
                Metadata: new Dictionary<string, object>
                {
                    ["stochK"] = stoch.K,
                    ["stochD"] = stoch.D
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }
        else if (stoch.K > 80 && stoch.D > 80 && stoch.K < stoch.D)
        {
            signals.Add(new SignalDto(
                Symbol: symbol,
                Timeframe: timeframe,
                Type: "CONFIRMATION",
                Direction: "SHORT",
                Confidence: 70m,
                EntryPrice: currentPrice,
                StopLoss: null,
                TakeProfit: null,
                Strategy: "Momentum",
                Reason: $"Stochastic overbought reversal (K: {stoch.K:F1}, D: {stoch.D:F1})",
                Metadata: new Dictionary<string, object>
                {
                    ["stochK"] = stoch.K,
                    ["stochD"] = stoch.D
                },
                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            ));
        }

        // OBV Momentum
        if (indicators.Obv.Count >= 20)
        {
            var obvValues = indicators.Obv.TakeLast(20).ToList();
            var obvSlope = (obvValues.Last().Value - obvValues.First().Value) / 20;

            if (obvSlope > 0 && currentPrice > indicators.Ema21.Last().Value)
            {
                signals.Add(new SignalDto(
                    Symbol: symbol,
                    Timeframe: timeframe,
                    Type: "CONFIRMATION",
                    Direction: "LONG",
                    Confidence: 65m,
                    EntryPrice: currentPrice,
                    StopLoss: null,
                    TakeProfit: null,
                    Strategy: "Momentum",
                    Reason: "Positive OBV trend with price above EMA 21",
                    Metadata: new Dictionary<string, object>
                    {
                        ["obvSlope"] = obvSlope,
                        ["currentObv"] = obvValues.Last().Value
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        return signals;
    }

    private decimal CalculateStrategyScore(IReadOnlyList<SignalDto> signals)
    {
        if (signals.Count == 0) return 0;

        var longScore = signals.Where(s => s.Direction == "LONG").Sum(s => s.Confidence);
        var shortScore = signals.Where(s => s.Direction == "SHORT").Sum(s => s.Confidence);

        return longScore - shortScore;
    }

    private decimal CalculateTrendConfidence(decimal ema9, decimal ema21, decimal ema50, decimal price, bool bullish)
    {
        if (bullish)
        {
            var alignment = (ema9 > ema21 && ema21 > ema50) ? 20m : 0m;
            var pricePosition = ((price - ema50) / ema50) * 100m;
            var separation = ((ema9 - ema21) / ema21) * 1000m;

            return Math.Min(50m + alignment + Math.Min(pricePosition * 2, 20m) + Math.Min(separation * 2, 10m), 95m);
        }
        else
        {
            var alignment = (ema9 < ema21 && ema21 < ema50) ? 20m : 0m;
            var pricePosition = ((ema50 - price) / ema50) * 100m;
            var separation = ((ema21 - ema9) / ema21) * 1000m;

            return Math.Min(50m + alignment + Math.Min(pricePosition * 2, 20m) + Math.Min(separation * 2, 10m), 95m);
        }
    }
}
