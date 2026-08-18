using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.API.Services;

public class PatternRecognitionService : IPatternRecognitionService
{
    private readonly IMarketDataService _marketDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PatternRecognitionService> _logger;

    public PatternRecognitionService(
        IMarketDataService marketDataService,
        IMemoryCache cache,
        ILogger<PatternRecognitionService> logger)
    {
        _marketDataService = marketDataService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PatternRecognitionResponse> GetPatternsAsync(string symbol, string timeframe, int limit = 500)
    {
        var cacheKey = $"patterns:{symbol}:{timeframe}:{limit}";
        if (_cache.TryGetValue(cacheKey, out PatternRecognitionResponse? cached))
        {
            return cached!;
        }

        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);

        var candlePatterns = DetectCandlePatterns(candles);
        var chartPatterns = DetectChartPatterns(candles);

        var response = new PatternRecognitionResponse(
            Symbol: symbol,
            Timeframe: timeframe,
            CandlePatterns: candlePatterns,
            ChartPatterns: chartPatterns,
            LastUpdated: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        return response;
    }

    public async Task<IReadOnlyList<CandlePatternDto>> GetCandlePatternsAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectCandlePatterns(candles);
    }

    public async Task<IReadOnlyList<ChartPatternDto>> GetChartPatternsAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectChartPatterns(candles);
    }

    public async Task<IReadOnlyList<PatternAlertDto>> GetRecentAlertsAsync(string symbol, string timeframe, int hours = 24)
    {
        var patterns = await GetPatternsAsync(symbol, timeframe, 500);
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds();

        var alerts = new List<PatternAlertDto>();

        // Add alerts from candle patterns
        foreach (var pattern in patterns.CandlePatterns.Where(p => p.Timestamp >= cutoffTime && p.Confidence >= 70))
        {
            alerts.Add(new PatternAlertDto(
                Symbol: symbol,
                Timeframe: timeframe,
                PatternType: pattern.Type,
                Direction: pattern.Direction,
                Confidence: pattern.Confidence,
                Message: $"{pattern.Direction} {pattern.Name} detected with {pattern.Confidence:F1}% confidence",
                Timestamp: pattern.Timestamp
            ));
        }

        // Add alerts from chart patterns
        foreach (var pattern in patterns.ChartPatterns.Where(p => p.EndTimestamp >= cutoffTime && p.Confidence >= 70))
        {
            alerts.Add(new PatternAlertDto(
                Symbol: symbol,
                Timeframe: timeframe,
                PatternType: pattern.Type,
                Direction: pattern.Direction,
                Confidence: pattern.Confidence,
                Message: $"{pattern.Direction} {pattern.Name} detected with {pattern.Confidence:F1}% confidence",
                Timestamp: pattern.EndTimestamp
            ));
        }

        return alerts.OrderByDescending(a => a.Timestamp).ToList();
    }

    #region Candle Pattern Detection

    private List<CandlePatternDto> DetectCandlePatterns(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<CandlePatternDto>();

        if (candles.Count < 3) return patterns;

        // Single candle patterns
        for (int i = 0; i < candles.Count; i++)
        {
            patterns.AddRange(DetectSingleCandlePatterns(candles, i));
        }

        // Two candle patterns
        for (int i = 1; i < candles.Count; i++)
        {
            patterns.AddRange(DetectTwoCandlePatterns(candles, i));
        }

        // Three candle patterns
        for (int i = 2; i < candles.Count; i++)
        {
            patterns.AddRange(DetectThreeCandlePatterns(candles, i));
        }

        return patterns.OrderByDescending(p => p.Timestamp).ToList();
    }

    private List<CandlePatternDto> DetectSingleCandlePatterns(IReadOnlyList<CandleDto> candles, int index)
    {
        var patterns = new List<CandlePatternDto>();
        var candle = candles[index];

        var body = Math.Abs(candle.Close - candle.Open);
        var range = candle.High - candle.Low;
        var upperShadow = candle.High - Math.Max(candle.Open, candle.Close);
        var lowerShadow = Math.Min(candle.Open, candle.Close) - candle.Low;

        // Doji - body is very small compared to range
        if (range > 0 && body / range < 0.1m)
        {
            var dojiType = PatternType.Doji;
            var direction = PatternDirection.Neutral;
            var name = "Doji";
            var description = "Indecision in the market";

            // Dragonfly Doji - long lower shadow, no upper shadow
            if (lowerShadow > body * 2 && upperShadow < body)
            {
                dojiType = PatternType.DragonflyDoji;
                direction = PatternDirection.Bullish;
                name = "Dragonfly Doji";
                description = "Potential bullish reversal with strong rejection of lower prices";
            }
            // Gravestone Doji - long upper shadow, no lower shadow
            else if (upperShadow > body * 2 && lowerShadow < body)
            {
                dojiType = PatternType.GravestoneDoji;
                direction = PatternDirection.Bearish;
                name = "Gravestone Doji";
                description = "Potential bearish reversal with strong rejection of higher prices";
            }
            // Long-legged Doji - both shadows are long
            else if (upperShadow > body && lowerShadow > body)
            {
                dojiType = PatternType.LongLeggedDoji;
                name = "Long-Legged Doji";
                description = "High volatility and indecision";
            }

            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: dojiType,
                Direction: direction,
                Name: name,
                Confidence: 75m,
                Description: description,
                Metadata: new Dictionary<string, object>
                {
                    { "bodyToRangeRatio", body / range },
                    { "upperShadow", upperShadow },
                    { "lowerShadow", lowerShadow }
                }
            ));
        }

        // Hammer - small body at top, long lower shadow, little to no upper shadow
        if (range > 0 && body / range < 0.3m && lowerShadow > body * 2 && upperShadow < body * 0.5m)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.Hammer,
                Direction: PatternDirection.Bullish,
                Name: "Hammer",
                Confidence: 80m,
                Description: "Potential bullish reversal after downtrend",
                Metadata: new Dictionary<string, object>
                {
                    { "lowerShadowRatio", lowerShadow / body },
                    { "bodyPosition", "upper" }
                }
            ));
        }

        // Inverted Hammer - small body at bottom, long upper shadow
        if (range > 0 && body / range < 0.3m && upperShadow > body * 2 && lowerShadow < body * 0.5m)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.InvertedHammer,
                Direction: PatternDirection.Bullish,
                Name: "Inverted Hammer",
                Confidence: 75m,
                Description: "Potential bullish reversal with buyers trying to push prices higher",
                Metadata: new Dictionary<string, object>
                {
                    { "upperShadowRatio", upperShadow / body },
                    { "bodyPosition", "lower" }
                }
            ));
        }

        // Shooting Star - small body at bottom, long upper shadow (bearish)
        if (range > 0 && body / range < 0.3m && upperShadow > body * 2 && lowerShadow < body * 0.5m && candle.Close < candle.Open)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.ShootingStar,
                Direction: PatternDirection.Bearish,
                Name: "Shooting Star",
                Confidence: 80m,
                Description: "Potential bearish reversal after uptrend",
                Metadata: new Dictionary<string, object>
                {
                    { "upperShadowRatio", upperShadow / body },
                    { "rejectionLevel", candle.High }
                }
            ));
        }

        // Hanging Man - small body at top, long lower shadow (bearish at top of uptrend)
        if (range > 0 && body / range < 0.3m && lowerShadow > body * 2 && upperShadow < body * 0.5m && candle.Close < candle.Open)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.HangingMan,
                Direction: PatternDirection.Bearish,
                Name: "Hanging Man",
                Confidence: 75m,
                Description: "Potential bearish reversal at top of uptrend",
                Metadata: new Dictionary<string, object>
                {
                    { "lowerShadowRatio", lowerShadow / body },
                    { "supportTest", candle.Low }
                }
            ));
        }

        // Marubozu - very small shadows, large body
        if (range > 0 && body / range > 0.9m)
        {
            var direction = candle.Close > candle.Open ? PatternDirection.Bullish : PatternDirection.Bearish;
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.Marubozu,
                Direction: direction,
                Name: $"{direction} Marubozu",
                Confidence: 85m,
                Description: $"Strong {direction.ToString().ToLower()} momentum with no hesitation",
                Metadata: new Dictionary<string, object>
                {
                    { "bodyToRangeRatio", body / range },
                    { "momentum", "strong" }
                }
            ));
        }

        // Spinning Top - small body with long shadows on both sides
        if (range > 0 && body / range < 0.3m && upperShadow > body && lowerShadow > body)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: candle.Timestamp,
                Type: PatternType.SpinningTop,
                Direction: PatternDirection.Neutral,
                Name: "Spinning Top",
                Confidence: 70m,
                Description: "Indecision with battles between bulls and bears",
                Metadata: new Dictionary<string, object>
                {
                    { "upperShadow", upperShadow },
                    { "lowerShadow", lowerShadow },
                    { "bodySize", body }
                }
            ));
        }

        return patterns;
    }

    private List<CandlePatternDto> DetectTwoCandlePatterns(IReadOnlyList<CandleDto> candles, int index)
    {
        var patterns = new List<CandlePatternDto>();
        var current = candles[index];
        var previous = candles[index - 1];

        var currentBody = Math.Abs(current.Close - current.Open);
        var previousBody = Math.Abs(previous.Close - previous.Open);

        // Bullish Engulfing
        if (previous.Close < previous.Open && // Previous is bearish
            current.Close > current.Open && // Current is bullish
            current.Open < previous.Close && // Current opens below previous close
            current.Close > previous.Open) // Current closes above previous open
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.BullishEngulfing,
                Direction: PatternDirection.Bullish,
                Name: "Bullish Engulfing",
                Confidence: 85m,
                Description: "Strong bullish reversal pattern",
                Metadata: new Dictionary<string, object>
                {
                    { "engulfmentRatio", currentBody / previousBody },
                    { "previousClose", previous.Close },
                    { "currentClose", current.Close }
                }
            ));
        }

        // Bearish Engulfing
        if (previous.Close > previous.Open && // Previous is bullish
            current.Close < current.Open && // Current is bearish
            current.Open > previous.Close && // Current opens above previous close
            current.Close < previous.Open) // Current closes below previous open
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.BearishEngulfing,
                Direction: PatternDirection.Bearish,
                Name: "Bearish Engulfing",
                Confidence: 85m,
                Description: "Strong bearish reversal pattern",
                Metadata: new Dictionary<string, object>
                {
                    { "engulfmentRatio", currentBody / previousBody },
                    { "previousClose", previous.Close },
                    { "currentClose", current.Close }
                }
            ));
        }

        // Bullish Harami
        if (previous.Close < previous.Open && // Previous is bearish
            current.Close > current.Open && // Current is bullish
            current.Open > previous.Close && // Current opens above previous close
            current.Close < previous.Open && // Current closes below previous open
            currentBody < previousBody * 0.5m) // Current body is smaller
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.BullishHarami,
                Direction: PatternDirection.Bullish,
                Name: "Bullish Harami",
                Confidence: 75m,
                Description: "Potential bullish reversal with decreasing bearish momentum",
                Metadata: new Dictionary<string, object>
                {
                    { "bodySizeRatio", currentBody / previousBody },
                    { "containment", "full" }
                }
            ));
        }

        // Bearish Harami
        if (previous.Close > previous.Open && // Previous is bullish
            current.Close < current.Open && // Current is bearish
            current.Open < previous.Close && // Current opens below previous close
            current.Close > previous.Open && // Current closes above previous open
            currentBody < previousBody * 0.5m) // Current body is smaller
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.BearishHarami,
                Direction: PatternDirection.Bearish,
                Name: "Bearish Harami",
                Confidence: 75m,
                Description: "Potential bearish reversal with decreasing bullish momentum",
                Metadata: new Dictionary<string, object>
                {
                    { "bodySizeRatio", currentBody / previousBody },
                    { "containment", "full" }
                }
            ));
        }

        // Piercing Pattern
        if (previous.Close < previous.Open && // Previous is bearish
            current.Close > current.Open && // Current is bullish
            current.Open < previous.Low && // Current opens below previous low
            current.Close > (previous.Open + previous.Close) / 2 && // Current closes above midpoint
            current.Close < previous.Open) // But below previous open
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.PiercingPattern,
                Direction: PatternDirection.Bullish,
                Name: "Piercing Pattern",
                Confidence: 80m,
                Description: "Bullish reversal with strong buying pressure",
                Metadata: new Dictionary<string, object>
                {
                    { "penetrationLevel", (current.Close - previous.Close) / previousBody },
                    { "gapDown", previous.Low - current.Open }
                }
            ));
        }

        // Dark Cloud Cover
        if (previous.Close > previous.Open && // Previous is bullish
            current.Close < current.Open && // Current is bearish
            current.Open > previous.High && // Current opens above previous high
            current.Close < (previous.Open + previous.Close) / 2 && // Current closes below midpoint
            current.Close > previous.Open) // But above previous open
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.DarkCloudCover,
                Direction: PatternDirection.Bearish,
                Name: "Dark Cloud Cover",
                Confidence: 80m,
                Description: "Bearish reversal with strong selling pressure",
                Metadata: new Dictionary<string, object>
                {
                    { "penetrationLevel", (previous.Close - current.Close) / previousBody },
                    { "gapUp", current.Open - previous.High }
                }
            ));
        }

        // Tweezer Top
        if (previous.Close > previous.Open && // Previous is bullish
            current.Close < current.Open && // Current is bearish
            Math.Abs(previous.High - current.High) < (previous.High * 0.001m)) // Highs are very close
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.TweezerTop,
                Direction: PatternDirection.Bearish,
                Name: "Tweezer Top",
                Confidence: 75m,
                Description: "Bearish reversal at resistance level",
                Metadata: new Dictionary<string, object>
                {
                    { "resistanceLevel", current.High },
                    { "highDifference", Math.Abs(previous.High - current.High) }
                }
            ));
        }

        // Tweezer Bottom
        if (previous.Close < previous.Open && // Previous is bearish
            current.Close > current.Open && // Current is bullish
            Math.Abs(previous.Low - current.Low) < (previous.Low * 0.001m)) // Lows are very close
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.TweezerBottom,
                Direction: PatternDirection.Bullish,
                Name: "Tweezer Bottom",
                Confidence: 75m,
                Description: "Bullish reversal at support level",
                Metadata: new Dictionary<string, object>
                {
                    { "supportLevel", current.Low },
                    { "lowDifference", Math.Abs(previous.Low - current.Low) }
                }
            ));
        }

        return patterns;
    }

    private List<CandlePatternDto> DetectThreeCandlePatterns(IReadOnlyList<CandleDto> candles, int index)
    {
        var patterns = new List<CandlePatternDto>();
        var current = candles[index];
        var middle = candles[index - 1];
        var first = candles[index - 2];

        // Morning Star
        if (first.Close < first.Open && // First is bearish
            Math.Abs(middle.Close - middle.Open) < (first.Close - first.Open) * 0.3m && // Middle is small
            current.Close > current.Open && // Current is bullish
            current.Close > (first.Open + first.Close) / 2) // Current closes above midpoint of first
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.MorningStar,
                Direction: PatternDirection.Bullish,
                Name: "Morning Star",
                Confidence: 85m,
                Description: "Strong bullish reversal pattern",
                Metadata: new Dictionary<string, object>
                {
                    { "starBody", Math.Abs(middle.Close - middle.Open) },
                    { "confirmationClose", current.Close },
                    { "pattern", "three-candle-reversal" }
                }
            ));
        }

        // Evening Star
        if (first.Close > first.Open && // First is bullish
            Math.Abs(middle.Close - middle.Open) < (first.Close - first.Open) * 0.3m && // Middle is small
            current.Close < current.Open && // Current is bearish
            current.Close < (first.Open + first.Close) / 2) // Current closes below midpoint of first
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.EveningStar,
                Direction: PatternDirection.Bearish,
                Name: "Evening Star",
                Confidence: 85m,
                Description: "Strong bearish reversal pattern",
                Metadata: new Dictionary<string, object>
                {
                    { "starBody", Math.Abs(middle.Close - middle.Open) },
                    { "confirmationClose", current.Close },
                    { "pattern", "three-candle-reversal" }
                }
            ));
        }

        // Three White Soldiers
        if (first.Close > first.Open && middle.Close > middle.Open && current.Close > current.Open && // All bullish
            middle.Close > first.Close && current.Close > middle.Close && // Progressive closes
            middle.Open > first.Open && middle.Open < first.Close && // Opens within previous body
            current.Open > middle.Open && current.Open < middle.Close)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.ThreeWhiteSoldiers,
                Direction: PatternDirection.Bullish,
                Name: "Three White Soldiers",
                Confidence: 90m,
                Description: "Strong bullish continuation with sustained buying pressure",
                Metadata: new Dictionary<string, object>
                {
                    { "priceProgress", new[] { first.Close, middle.Close, current.Close } },
                    { "strength", "very-strong" }
                }
            ));
        }

        // Three Black Crows
        if (first.Close < first.Open && middle.Close < middle.Open && current.Close < current.Open && // All bearish
            middle.Close < first.Close && current.Close < middle.Close && // Progressive closes
            middle.Open < first.Open && middle.Open > first.Close && // Opens within previous body
            current.Open < middle.Open && current.Open > middle.Close)
        {
            patterns.Add(new CandlePatternDto(
                Timestamp: current.Timestamp,
                Type: PatternType.ThreeBlackCrows,
                Direction: PatternDirection.Bearish,
                Name: "Three Black Crows",
                Confidence: 90m,
                Description: "Strong bearish continuation with sustained selling pressure",
                Metadata: new Dictionary<string, object>
                {
                    { "priceProgress", new[] { first.Close, middle.Close, current.Close } },
                    { "strength", "very-strong" }
                }
            ));
        }

        return patterns;
    }

    #endregion

    #region Chart Pattern Detection

    private List<ChartPatternDto> DetectChartPatterns(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<ChartPatternDto>();

        if (candles.Count < 20) return patterns;

        // Detect various chart patterns
        patterns.AddRange(DetectHeadAndShoulders(candles));
        patterns.AddRange(DetectDoubleTopBottom(candles));
        patterns.AddRange(DetectTriangles(candles));
        patterns.AddRange(DetectFlagsAndPennants(candles));

        return patterns.OrderByDescending(p => p.EndTimestamp).ToList();
    }

    private List<ChartPatternDto> DetectHeadAndShoulders(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<ChartPatternDto>();
        if (candles.Count < 30) return patterns;

        // Simplified head and shoulders detection
        var recentCandles = candles.TakeLast(50).ToList();
        var highs = recentCandles.Select(c => c.High).ToList();
        var lows = recentCandles.Select(c => c.Low).ToList();

        // Find local peaks
        var peaks = new List<(int Index, decimal Price)>();
        for (int i = 5; i < recentCandles.Count - 5; i++)
        {
            if (recentCandles[i].High > recentCandles[i - 1].High &&
                recentCandles[i].High > recentCandles[i + 1].High &&
                recentCandles[i].High > recentCandles[i - 2].High &&
                recentCandles[i].High > recentCandles[i + 2].High)
            {
                peaks.Add((i, recentCandles[i].High));
            }
        }

        // Need at least 3 peaks for head and shoulders
        if (peaks.Count >= 3)
        {
            var lastThreePeaks = peaks.TakeLast(3).ToList();
            var leftShoulder = lastThreePeaks[0];
            var head = lastThreePeaks[1];
            var rightShoulder = lastThreePeaks[2];

            // Check if middle peak (head) is highest
            if (head.Price > leftShoulder.Price && head.Price > rightShoulder.Price &&
                Math.Abs(leftShoulder.Price - rightShoulder.Price) / leftShoulder.Price < 0.03m)
            {
                var neckline = Math.Min(
                    recentCandles.Skip(leftShoulder.Index).Take(head.Index - leftShoulder.Index).Min(c => c.Low),
                    recentCandles.Skip(head.Index).Take(rightShoulder.Index - head.Index).Min(c => c.Low)
                );

                var patternHeight = head.Price - neckline;
                var targetPrice = neckline - patternHeight;

                patterns.Add(new ChartPatternDto(
                    StartTimestamp: recentCandles[leftShoulder.Index].Timestamp,
                    EndTimestamp: recentCandles[rightShoulder.Index].Timestamp,
                    Type: PatternType.HeadAndShoulders,
                    Direction: PatternDirection.Bearish,
                    Name: "Head and Shoulders",
                    Confidence: 80m,
                    Description: "Bearish reversal pattern with three peaks",
                    Metadata: new Dictionary<string, object>
                    {
                        { "leftShoulderPrice", leftShoulder.Price },
                        { "headPrice", head.Price },
                        { "rightShoulderPrice", rightShoulder.Price },
                        { "neckline", neckline },
                        { "patternHeight", patternHeight }
                    },
                    BreakoutPrice: neckline,
                    TargetPrice: targetPrice,
                    StopLossPrice: head.Price
                ));
            }
        }

        return patterns;
    }

    private List<ChartPatternDto> DetectDoubleTopBottom(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<ChartPatternDto>();
        if (candles.Count < 30) return patterns;

        var recentCandles = candles.TakeLast(40).ToList();

        // Find local peaks for double top
        var peaks = new List<(int Index, decimal Price)>();
        for (int i = 5; i < recentCandles.Count - 5; i++)
        {
            if (recentCandles[i].High > recentCandles[i - 1].High &&
                recentCandles[i].High > recentCandles[i + 1].High)
            {
                peaks.Add((i, recentCandles[i].High));
            }
        }

        // Double Top
        if (peaks.Count >= 2)
        {
            var lastTwoPeaks = peaks.TakeLast(2).ToList();
            var firstPeak = lastTwoPeaks[0];
            var secondPeak = lastTwoPeaks[1];

            // Peaks should be at similar levels
            if (Math.Abs(firstPeak.Price - secondPeak.Price) / firstPeak.Price < 0.02m)
            {
                var troughBetween = recentCandles
                    .Skip(firstPeak.Index)
                    .Take(secondPeak.Index - firstPeak.Index)
                    .Min(c => c.Low);

                var patternHeight = firstPeak.Price - troughBetween;
                var targetPrice = troughBetween - patternHeight;

                patterns.Add(new ChartPatternDto(
                    StartTimestamp: recentCandles[firstPeak.Index].Timestamp,
                    EndTimestamp: recentCandles[secondPeak.Index].Timestamp,
                    Type: PatternType.DoubleTop,
                    Direction: PatternDirection.Bearish,
                    Name: "Double Top",
                    Confidence: 75m,
                    Description: "Bearish reversal with two resistance tests",
                    Metadata: new Dictionary<string, object>
                    {
                        { "firstPeakPrice", firstPeak.Price },
                        { "secondPeakPrice", secondPeak.Price },
                        { "supportLevel", troughBetween },
                        { "patternHeight", patternHeight }
                    },
                    BreakoutPrice: troughBetween,
                    TargetPrice: targetPrice,
                    StopLossPrice: firstPeak.Price
                ));
            }
        }

        // Find local troughs for double bottom
        var troughs = new List<(int Index, decimal Price)>();
        for (int i = 5; i < recentCandles.Count - 5; i++)
        {
            if (recentCandles[i].Low < recentCandles[i - 1].Low &&
                recentCandles[i].Low < recentCandles[i + 1].Low)
            {
                troughs.Add((i, recentCandles[i].Low));
            }
        }

        // Double Bottom
        if (troughs.Count >= 2)
        {
            var lastTwoTroughs = troughs.TakeLast(2).ToList();
            var firstTrough = lastTwoTroughs[0];
            var secondTrough = lastTwoTroughs[1];

            // Troughs should be at similar levels
            if (Math.Abs(firstTrough.Price - secondTrough.Price) / firstTrough.Price < 0.02m)
            {
                var peakBetween = recentCandles
                    .Skip(firstTrough.Index)
                    .Take(secondTrough.Index - firstTrough.Index)
                    .Max(c => c.High);

                var patternHeight = peakBetween - firstTrough.Price;
                var targetPrice = peakBetween + patternHeight;

                patterns.Add(new ChartPatternDto(
                    StartTimestamp: recentCandles[firstTrough.Index].Timestamp,
                    EndTimestamp: recentCandles[secondTrough.Index].Timestamp,
                    Type: PatternType.DoubleBottom,
                    Direction: PatternDirection.Bullish,
                    Name: "Double Bottom",
                    Confidence: 75m,
                    Description: "Bullish reversal with two support tests",
                    Metadata: new Dictionary<string, object>
                    {
                        { "firstTroughPrice", firstTrough.Price },
                        { "secondTroughPrice", secondTrough.Price },
                        { "resistanceLevel", peakBetween },
                        { "patternHeight", patternHeight }
                    },
                    BreakoutPrice: peakBetween,
                    TargetPrice: targetPrice,
                    StopLossPrice: firstTrough.Price
                ));
            }
        }

        return patterns;
    }

    private List<ChartPatternDto> DetectTriangles(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<ChartPatternDto>();
        if (candles.Count < 20) return patterns;

        var recentCandles = candles.TakeLast(30).ToList();
        var highs = recentCandles.Select(c => c.High).ToList();
        var lows = recentCandles.Select(c => c.Low).ToList();

        // Calculate trend lines using simple linear regression
        var highSlope = CalculateSlope(highs);
        var lowSlope = CalculateSlope(lows);

        // Ascending Triangle - horizontal resistance, rising support
        if (Math.Abs(highSlope) < 0.0001m && lowSlope > 0.001m)
        {
            var resistance = highs.Max();
            var initialSupport = lows.First();
            var targetPrice = resistance + (resistance - initialSupport);

            patterns.Add(new ChartPatternDto(
                StartTimestamp: recentCandles.First().Timestamp,
                EndTimestamp: recentCandles.Last().Timestamp,
                Type: PatternType.AscendingTriangle,
                Direction: PatternDirection.Bullish,
                Name: "Ascending Triangle",
                Confidence: 70m,
                Description: "Bullish continuation pattern with horizontal resistance",
                Metadata: new Dictionary<string, object>
                {
                    { "resistanceLevel", resistance },
                    { "supportSlope", lowSlope },
                    { "patternType", "continuation" }
                },
                BreakoutPrice: resistance,
                TargetPrice: targetPrice,
                StopLossPrice: lows.Min()
            ));
        }

        // Descending Triangle - declining resistance, horizontal support
        if (highSlope < -0.001m && Math.Abs(lowSlope) < 0.0001m)
        {
            var support = lows.Min();
            var initialResistance = highs.First();
            var targetPrice = support - (initialResistance - support);

            patterns.Add(new ChartPatternDto(
                StartTimestamp: recentCandles.First().Timestamp,
                EndTimestamp: recentCandles.Last().Timestamp,
                Type: PatternType.DescendingTriangle,
                Direction: PatternDirection.Bearish,
                Name: "Descending Triangle",
                Confidence: 70m,
                Description: "Bearish continuation pattern with horizontal support",
                Metadata: new Dictionary<string, object>
                {
                    { "supportLevel", support },
                    { "resistanceSlope", highSlope },
                    { "patternType", "continuation" }
                },
                BreakoutPrice: support,
                TargetPrice: targetPrice,
                StopLossPrice: highs.Max()
            ));
        }

        // Symmetrical Triangle - converging trend lines
        if (highSlope < -0.001m && lowSlope > 0.001m)
        {
            var apex = (highs.Last() + lows.Last()) / 2;
            var patternHeight = highs.First() - lows.First();
            var targetUp = apex + patternHeight * 0.75m;
            var targetDown = apex - patternHeight * 0.75m;

            patterns.Add(new ChartPatternDto(
                StartTimestamp: recentCandles.First().Timestamp,
                EndTimestamp: recentCandles.Last().Timestamp,
                Type: PatternType.SymmetricalTriangle,
                Direction: PatternDirection.Neutral,
                Name: "Symmetrical Triangle",
                Confidence: 65m,
                Description: "Neutral pattern awaiting breakout direction",
                Metadata: new Dictionary<string, object>
                {
                    { "apexPrice", apex },
                    { "resistanceSlope", highSlope },
                    { "supportSlope", lowSlope },
                    { "patternHeight", patternHeight }
                },
                BreakoutPrice: apex,
                TargetPrice: targetUp,
                StopLossPrice: targetDown
            ));
        }

        return patterns;
    }

    private List<ChartPatternDto> DetectFlagsAndPennants(IReadOnlyList<CandleDto> candles)
    {
        var patterns = new List<ChartPatternDto>();
        if (candles.Count < 15) return patterns;

        var recentCandles = candles.TakeLast(20).ToList();

        // Check for strong preceding trend (pole)
        var poleStart = candles.Count - 20;
        var poleEnd = candles.Count - 10;
        if (poleStart < 0) return patterns;

        var poleCandles = candles.Skip(poleStart).Take(10).ToList();
        var consolidationCandles = candles.Skip(poleEnd).Take(10).ToList();

        var poleChange = (poleCandles.Last().Close - poleCandles.First().Open) / poleCandles.First().Open;

        // Bullish Flag - upward pole followed by downward consolidation
        if (poleChange > 0.05m)
        {
            var consolidationSlope = CalculateSlope(consolidationCandles.Select(c => c.Close).ToList());

            if (consolidationSlope < -0.001m)
            {
                var poleHeight = poleCandles.Last().Close - poleCandles.First().Open;
                var breakoutPrice = consolidationCandles.Last().Close;
                var targetPrice = breakoutPrice + poleHeight;

                patterns.Add(new ChartPatternDto(
                    StartTimestamp: poleCandles.First().Timestamp,
                    EndTimestamp: consolidationCandles.Last().Timestamp,
                    Type: PatternType.FlagBullish,
                    Direction: PatternDirection.Bullish,
                    Name: "Bullish Flag",
                    Confidence: 75m,
                    Description: "Bullish continuation after consolidation",
                    Metadata: new Dictionary<string, object>
                    {
                        { "poleHeight", poleHeight },
                        { "consolidationSlope", consolidationSlope },
                        { "poleStartPrice", poleCandles.First().Open }
                    },
                    BreakoutPrice: breakoutPrice,
                    TargetPrice: targetPrice,
                    StopLossPrice: consolidationCandles.Min(c => c.Low)
                ));
            }
        }

        // Bearish Flag - downward pole followed by upward consolidation
        if (poleChange < -0.05m)
        {
            var consolidationSlope = CalculateSlope(consolidationCandles.Select(c => c.Close).ToList());

            if (consolidationSlope > 0.001m)
            {
                var poleHeight = poleCandles.First().Open - poleCandles.Last().Close;
                var breakoutPrice = consolidationCandles.Last().Close;
                var targetPrice = breakoutPrice - poleHeight;

                patterns.Add(new ChartPatternDto(
                    StartTimestamp: poleCandles.First().Timestamp,
                    EndTimestamp: consolidationCandles.Last().Timestamp,
                    Type: PatternType.FlagBearish,
                    Direction: PatternDirection.Bearish,
                    Name: "Bearish Flag",
                    Confidence: 75m,
                    Description: "Bearish continuation after consolidation",
                    Metadata: new Dictionary<string, object>
                    {
                        { "poleHeight", poleHeight },
                        { "consolidationSlope", consolidationSlope },
                        { "poleStartPrice", poleCandles.First().Open }
                    },
                    BreakoutPrice: breakoutPrice,
                    TargetPrice: targetPrice,
                    StopLossPrice: consolidationCandles.Max(c => c.High)
                ));
            }
        }

        return patterns;
    }

    private decimal CalculateSlope(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2) return 0;

        var n = values.Count;
        var sumX = 0m;
        var sumY = 0m;
        var sumXY = 0m;
        var sumX2 = 0m;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope / (sumY / n); // Normalize by average price
    }

    #endregion
}
