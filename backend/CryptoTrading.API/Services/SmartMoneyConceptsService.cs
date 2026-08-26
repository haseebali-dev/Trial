using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.API.Services;

public class SmartMoneyConceptsService : ISmartMoneyConceptsService
{
    private readonly IMarketDataService _marketDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SmartMoneyConceptsService> _logger;

    public SmartMoneyConceptsService(
        IMarketDataService marketDataService,
        IMemoryCache cache,
        ILogger<SmartMoneyConceptsService> logger)
    {
        _marketDataService = marketDataService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SmcAnalysisResponse> GetSmcAnalysisAsync(string symbol, string timeframe, int limit = 500)
    {
        var cacheKey = $"smc:{symbol}:{timeframe}:{limit}";
        if (_cache.TryGetValue(cacheKey, out SmcAnalysisResponse? cached))
        {
            return cached!;
        }

        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);

        var orderBlocks = DetectOrderBlocks(candles);
        var fairValueGaps = DetectFairValueGaps(candles);
        var liquidityZones = DetectLiquidityZones(candles);
        var structureBreaks = DetectStructureBreaks(candles);
        var currentStructure = DetermineMarketStructure(candles);
        var premiumDiscountZone = CalculatePremiumDiscountZone(candles);

        var response = new SmcAnalysisResponse(
            Symbol: symbol,
            Timeframe: timeframe,
            CurrentStructure: currentStructure,
            OrderBlocks: orderBlocks,
            FairValueGaps: fairValueGaps,
            LiquidityZones: liquidityZones,
            StructureBreaks: structureBreaks,
            PremiumDiscountZone: premiumDiscountZone,
            CurrentPrice: candles.Last().Close,
            LastUpdated: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        return response;
    }

    public async Task<IReadOnlyList<OrderBlockDto>> GetOrderBlocksAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectOrderBlocks(candles);
    }

    public async Task<IReadOnlyList<FairValueGapDto>> GetFairValueGapsAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectFairValueGaps(candles);
    }

    public async Task<IReadOnlyList<LiquidityZoneDto>> GetLiquidityZonesAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectLiquidityZones(candles);
    }

    public async Task<IReadOnlyList<StructureBreakDto>> GetStructureBreaksAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return DetectStructureBreaks(candles);
    }

    public async Task<IReadOnlyList<SmcSignalDto>> GetSmcSignalsAsync(string symbol, string timeframe, int limit = 500)
    {
        var analysis = await GetSmcAnalysisAsync(symbol, timeframe, limit);
        return GenerateSmcSignals(analysis);
    }

    #region Order Blocks Detection

    private List<OrderBlockDto> DetectOrderBlocks(IReadOnlyList<CandleDto> candles)
    {
        var orderBlocks = new List<OrderBlockDto>();
        if (candles.Count < 10) return orderBlocks;

        // Look for strong impulsive moves followed by retracements
        for (int i = 3; i < candles.Count - 1; i++)
        {
            // Bullish Order Block: Strong down move, then bullish reversal
            if (IsBullishOrderBlock(candles, i))
            {
                var ob = CreateBullishOrderBlock(candles, i);
                if (ob != null) orderBlocks.Add(ob);
            }

            // Bearish Order Block: Strong up move, then bearish reversal
            if (IsBearishOrderBlock(candles, i))
            {
                var ob = CreateBearishOrderBlock(candles, i);
                if (ob != null) orderBlocks.Add(ob);
            }
        }

        // Filter and keep only the most significant order blocks
        return orderBlocks
            .OrderByDescending(ob => ob.Strength)
            .Take(20)
            .OrderByDescending(ob => ob.EndTimestamp)
            .ToList();
    }

    private bool IsBullishOrderBlock(IReadOnlyList<CandleDto> candles, int index)
    {
        if (index < 3 || index >= candles.Count - 2) return false;

        var current = candles[index];
        var prev1 = candles[index - 1];
        var prev2 = candles[index - 2];
        var next1 = candles[index + 1];
        var next2 = candles[index + 2];

        // Last bearish candle before bullish reversal with strong buying
        bool isLastBearish = current.Close < current.Open;
        bool hasStrongBullishFollowUp = next1.Close > next1.Open &&
                                        next1.Close > current.High &&
                                        (next1.Close - next1.Open) > (current.Open - current.Close) * 1.5m;

        // Verify downtrend before the order block
        bool hadDowntrend = prev2.Close < prev2.Open || prev1.Close < prev1.Open;

        return isLastBearish && hasStrongBullishFollowUp && hadDowntrend;
    }

    private bool IsBearishOrderBlock(IReadOnlyList<CandleDto> candles, int index)
    {
        if (index < 3 || index >= candles.Count - 2) return false;

        var current = candles[index];
        var prev1 = candles[index - 1];
        var prev2 = candles[index - 2];
        var next1 = candles[index + 1];
        var next2 = candles[index + 2];

        // Last bullish candle before bearish reversal with strong selling
        bool isLastBullish = current.Close > current.Open;
        bool hasStrongBearishFollowUp = next1.Close < next1.Open &&
                                         next1.Close < current.Low &&
                                         (next1.Open - next1.Close) > (current.Close - current.Open) * 1.5m;

        // Verify uptrend before the order block
        bool hadUptrend = prev2.Close > prev2.Open || prev1.Close > prev1.Open;

        return isLastBullish && hasStrongBearishFollowUp && hadUptrend;
    }

    private OrderBlockDto? CreateBullishOrderBlock(IReadOnlyList<CandleDto> candles, int index)
    {
        var orderBlockCandle = candles[index];
        var nextCandle = candles[index + 1];

        // Check if order block has been mitigated (price returned and closed above it)
        bool isMitigated = false;
        for (int i = index + 2; i < candles.Count; i++)
        {
            if (candles[i].Close < orderBlockCandle.Low)
            {
                isMitigated = true;
                break;
            }
        }

        // Calculate strength based on reaction and volume
        var strength = CalculateOrderBlockStrength(candles, index, true);

        return new OrderBlockDto(
            StartTimestamp: orderBlockCandle.Timestamp,
            EndTimestamp: nextCandle.Timestamp,
            Type: OrderBlockType.Bullish,
            HighPrice: orderBlockCandle.Open,
            LowPrice: orderBlockCandle.Low,
            Strength: strength,
            IsMitigated: isMitigated,
            Description: "Bullish order block - institutional buying zone",
            Metadata: new Dictionary<string, object>
            {
                { "reactionSize", nextCandle.High - orderBlockCandle.Low },
                { "blockSize", orderBlockCandle.Open - orderBlockCandle.Low },
                { "volume", orderBlockCandle.Volume }
            }
        );
    }

    private OrderBlockDto? CreateBearishOrderBlock(IReadOnlyList<CandleDto> candles, int index)
    {
        var orderBlockCandle = candles[index];
        var nextCandle = candles[index + 1];

        // Check if order block has been mitigated (price returned and closed below it)
        bool isMitigated = false;
        for (int i = index + 2; i < candles.Count; i++)
        {
            if (candles[i].Close > orderBlockCandle.High)
            {
                isMitigated = true;
                break;
            }
        }

        // Calculate strength based on reaction and volume
        var strength = CalculateOrderBlockStrength(candles, index, false);

        return new OrderBlockDto(
            StartTimestamp: orderBlockCandle.Timestamp,
            EndTimestamp: nextCandle.Timestamp,
            Type: OrderBlockType.Bearish,
            HighPrice: orderBlockCandle.High,
            LowPrice: orderBlockCandle.Open,
            Strength: strength,
            IsMitigated: isMitigated,
            Description: "Bearish order block - institutional selling zone",
            Metadata: new Dictionary<string, object>
            {
                { "reactionSize", orderBlockCandle.High - nextCandle.Low },
                { "blockSize", orderBlockCandle.High - orderBlockCandle.Open },
                { "volume", orderBlockCandle.Volume }
            }
        );
    }

    private decimal CalculateOrderBlockStrength(IReadOnlyList<CandleDto> candles, int index, bool isBullish)
    {
        var orderBlockCandle = candles[index];
        var nextCandle = candles[index + 1];

        // Factors: reaction size, volume, distance traveled
        var reactionSize = isBullish
            ? nextCandle.High - orderBlockCandle.Low
            : orderBlockCandle.High - nextCandle.Low;

        var blockSize = isBullish
            ? orderBlockCandle.Open - orderBlockCandle.Low
            : orderBlockCandle.High - orderBlockCandle.Open;

        var reactionRatio = blockSize == 0 ? 1 : reactionSize / blockSize;

        // Normalize volume (use relative volume)
        var avgVolume = candles.Skip(Math.Max(0, index - 20)).Take(20).Average(c => c.Volume);
        var volumeRatio = avgVolume == 0 ? 1 : orderBlockCandle.Volume / avgVolume;

        // Strength score (0-100)
        var strength = Math.Min(100m, (reactionRatio * 30m) + (volumeRatio * 30m) + 40m);

        return Math.Round(strength, 2);
    }

    #endregion

    #region Fair Value Gaps Detection

    private List<FairValueGapDto> DetectFairValueGaps(IReadOnlyList<CandleDto> candles)
    {
        var fvgs = new List<FairValueGapDto>();
        if (candles.Count < 3) return fvgs;

        for (int i = 1; i < candles.Count - 1; i++)
        {
            var prev = candles[i - 1];
            var current = candles[i];
            var next = candles[i + 1];

            // Bullish FVG: gap between prev.High and next.Low (with current in between)
            if (next.Low > prev.High)
            {
                var gapSize = next.Low - prev.High;
                var gapPercent = (gapSize / prev.High) * 100m;

                // Only consider significant gaps (> 0.1%)
                if (gapPercent > 0.1m)
                {
                    // Check if gap has been filled
                    bool isFilled = false;
                    decimal filledPercentage = 0m;

                    for (int j = i + 2; j < candles.Count; j++)
                    {
                        if (candles[j].Low <= prev.High)
                        {
                            isFilled = true;
                            filledPercentage = 100m;
                            break;
                        }
                        else if (candles[j].Low < next.Low)
                        {
                            filledPercentage = ((next.Low - candles[j].Low) / gapSize) * 100m;
                        }
                    }

                    fvgs.Add(new FairValueGapDto(
                        StartTimestamp: prev.Timestamp,
                        EndTimestamp: next.Timestamp,
                        Type: OrderBlockType.Bullish,
                        TopPrice: next.Low,
                        BottomPrice: prev.High,
                        GapSize: gapSize,
                        IsFilled: isFilled,
                        FilledPercentage: Math.Round(filledPercentage, 2),
                        Description: "Bullish Fair Value Gap - unfilled buying imbalance",
                        Metadata: new Dictionary<string, object>
                        {
                            { "gapPercent", Math.Round(gapPercent, 4) },
                            { "impulseSize", next.Close - prev.Close },
                            { "middleCandle", new { current.Open, current.Close, current.High, current.Low } }
                        }
                    ));
                }
            }

            // Bearish FVG: gap between next.High and prev.Low (with current in between)
            if (prev.Low > next.High)
            {
                var gapSize = prev.Low - next.High;
                var gapPercent = (gapSize / prev.Low) * 100m;

                // Only consider significant gaps (> 0.1%)
                if (gapPercent > 0.1m)
                {
                    // Check if gap has been filled
                    bool isFilled = false;
                    decimal filledPercentage = 0m;

                    for (int j = i + 2; j < candles.Count; j++)
                    {
                        if (candles[j].High >= prev.Low)
                        {
                            isFilled = true;
                            filledPercentage = 100m;
                            break;
                        }
                        else if (candles[j].High > next.High)
                        {
                            filledPercentage = ((candles[j].High - next.High) / gapSize) * 100m;
                        }
                    }

                    fvgs.Add(new FairValueGapDto(
                        StartTimestamp: next.Timestamp,
                        EndTimestamp: prev.Timestamp,
                        Type: OrderBlockType.Bearish,
                        TopPrice: prev.Low,
                        BottomPrice: next.High,
                        GapSize: gapSize,
                        IsFilled: isFilled,
                        FilledPercentage: Math.Round(filledPercentage, 2),
                        Description: "Bearish Fair Value Gap - unfilled selling imbalance",
                        Metadata: new Dictionary<string, object>
                        {
                            { "gapPercent", Math.Round(gapPercent, 4) },
                            { "impulseSize", prev.Close - next.Close },
                            { "middleCandle", new { current.Open, current.Close, current.High, current.Low } }
                        }
                    ));
                }
            }
        }

        return fvgs.OrderByDescending(fvg => fvg.EndTimestamp).Take(15).ToList();
    }

    #endregion

    #region Liquidity Zones Detection

    private List<LiquidityZoneDto> DetectLiquidityZones(IReadOnlyList<CandleDto> candles)
    {
        var liquidityZones = new List<LiquidityZoneDto>();
        if (candles.Count < 20) return liquidityZones;

        // Find significant highs (buy-side liquidity above)
        for (int i = 10; i < candles.Count - 10; i++)
        {
            var current = candles[i];
            var leftCandles = candles.Skip(i - 10).Take(10).ToList();
            var rightCandles = candles.Skip(i + 1).Take(10).ToList();

            // Check if it's a significant high
            bool isSignificantHigh = leftCandles.All(c => c.High < current.High) &&
                                     rightCandles.Take(5).All(c => c.High < current.High);

            if (isSignificantHigh)
            {
                // Check if liquidity was swept (price went above and closed back below)
                bool isSwept = false;
                for (int j = i + 1; j < candles.Count; j++)
                {
                    if (candles[j].High > current.High && candles[j].Close < current.High)
                    {
                        isSwept = true;
                        break;
                    }
                }

                // Calculate strength based on how many times price tested this level
                var strength = CalculateLiquidityStrength(candles, i, current.High, true);

                liquidityZones.Add(new LiquidityZoneDto(
                    Timestamp: current.Timestamp,
                    Type: LiquidityType.BuyLiquidity,
                    Price: current.High,
                    Strength: strength,
                    IsSwept: isSwept,
                    Description: "Buy-side liquidity above significant high",
                    Metadata: new Dictionary<string, object>
                    {
                        { "testCount", CountPriceTouches(candles, i, current.High, 0.002m) },
                        { "volume", current.Volume },
                        { "timesSinceCreation", candles.Count - i }
                    }
                ));
            }

            // Check if it's a significant low
            bool isSignificantLow = leftCandles.All(c => c.Low > current.Low) &&
                                    rightCandles.Take(5).All(c => c.Low > current.Low);

            if (isSignificantLow)
            {
                // Check if liquidity was swept (price went below and closed back above)
                bool isSwept = false;
                for (int j = i + 1; j < candles.Count; j++)
                {
                    if (candles[j].Low < current.Low && candles[j].Close > current.Low)
                    {
                        isSwept = true;
                        break;
                    }
                }

                // Calculate strength
                var strength = CalculateLiquidityStrength(candles, i, current.Low, false);

                liquidityZones.Add(new LiquidityZoneDto(
                    Timestamp: current.Timestamp,
                    Type: LiquidityType.SellLiquidity,
                    Price: current.Low,
                    Strength: strength,
                    IsSwept: isSwept,
                    Description: "Sell-side liquidity below significant low",
                    Metadata: new Dictionary<string, object>
                    {
                        { "testCount", CountPriceTouches(candles, i, current.Low, 0.002m) },
                        { "volume", current.Volume },
                        { "timesSinceCreation", candles.Count - i }
                    }
                ));
            }
        }

        return liquidityZones.OrderByDescending(lz => lz.Strength).Take(10).ToList();
    }

    private decimal CalculateLiquidityStrength(IReadOnlyList<CandleDto> candles, int index, decimal price, bool isHigh)
    {
        var touchCount = CountPriceTouches(candles, index, price, 0.002m);
        var volumeAtLevel = candles[index].Volume;
        var avgVolume = candles.Skip(Math.Max(0, index - 20)).Take(20).Average(c => c.Volume);
        var volumeRatio = avgVolume == 0 ? 1 : volumeAtLevel / avgVolume;

        // Strength based on touch count and volume
        var strength = Math.Min(100m, (touchCount * 20m) + (volumeRatio * 20m) + 30m);

        return Math.Round(strength, 2);
    }

    private int CountPriceTouches(IReadOnlyList<CandleDto> candles, int fromIndex, decimal price, decimal tolerance)
    {
        int count = 0;
        var toleranceRange = price * tolerance;

        for (int i = fromIndex + 1; i < candles.Count; i++)
        {
            if (Math.Abs(candles[i].High - price) <= toleranceRange ||
                Math.Abs(candles[i].Low - price) <= toleranceRange)
            {
                count++;
            }
        }

        return count;
    }

    #endregion

    #region Market Structure Detection

    private List<StructureBreakDto> DetectStructureBreaks(IReadOnlyList<CandleDto> candles)
    {
        var structureBreaks = new List<StructureBreakDto>();
        if (candles.Count < 30) return structureBreaks;

        var swingHighs = FindSwingHighs(candles);
        var swingLows = FindSwingLows(candles);

        // Analyze structure breaks
        for (int i = 20; i < candles.Count - 5; i++)
        {
            var recentHighs = swingHighs.Where(sh => sh.Index < i && sh.Index >= i - 20).OrderByDescending(sh => sh.Index).Take(3).ToList();
            var recentLows = swingLows.Where(sl => sl.Index < i && sl.Index >= i - 20).OrderByDescending(sl => sl.Index).Take(3).ToList();

            if (recentHighs.Count >= 2 && recentLows.Count >= 2)
            {
                // Check for BOS (Break of Structure) - continuation
                var bosBreak = DetectBOS(candles, i, recentHighs, recentLows);
                if (bosBreak != null) structureBreaks.Add(bosBreak);

                // Check for CHoCH (Change of Character) - reversal
                var chochBreak = DetectCHoCH(candles, i, recentHighs, recentLows);
                if (chochBreak != null) structureBreaks.Add(chochBreak);
            }
        }

        return structureBreaks.OrderByDescending(sb => sb.Timestamp).Take(10).ToList();
    }

    private StructureBreakDto? DetectBOS(IReadOnlyList<CandleDto> candles, int index, List<(int Index, decimal Price)> recentHighs, List<(int Index, decimal Price)> recentLows)
    {
        var current = candles[index];

        // Bullish BOS: price breaks above recent high while making higher lows
        if (recentHighs.Count >= 2 && recentLows.Count >= 2)
        {
            var lastHigh = recentHighs[0].Price;
            var isHigherLows = recentLows[0].Price > recentLows[1].Price;

            if (current.Close > lastHigh && isHigherLows)
            {
                return new StructureBreakDto(
                    Timestamp: current.Timestamp,
                    Type: StructureBreakType.BOS,
                    PreviousStructure: MarketStructure.Bullish,
                    NewStructure: MarketStructure.Bullish,
                    BreakPrice: current.Close,
                    PreviousHigh: lastHigh,
                    PreviousLow: recentLows[0].Price,
                    Description: "Bullish Break of Structure - continuation of uptrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "breakStrength", ((current.Close - lastHigh) / lastHigh) * 100m },
                        { "higherLows", isHigherLows }
                    }
                );
            }
        }

        // Bearish BOS: price breaks below recent low while making lower highs
        if (recentHighs.Count >= 2 && recentLows.Count >= 2)
        {
            var lastLow = recentLows[0].Price;
            var isLowerHighs = recentHighs[0].Price < recentHighs[1].Price;

            if (current.Close < lastLow && isLowerHighs)
            {
                return new StructureBreakDto(
                    Timestamp: current.Timestamp,
                    Type: StructureBreakType.BOS,
                    PreviousStructure: MarketStructure.Bearish,
                    NewStructure: MarketStructure.Bearish,
                    BreakPrice: current.Close,
                    PreviousHigh: recentHighs[0].Price,
                    PreviousLow: lastLow,
                    Description: "Bearish Break of Structure - continuation of downtrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "breakStrength", ((lastLow - current.Close) / lastLow) * 100m },
                        { "lowerHighs", isLowerHighs }
                    }
                );
            }
        }

        return null;
    }

    private StructureBreakDto? DetectCHoCH(IReadOnlyList<CandleDto> candles, int index, List<(int Index, decimal Price)> recentHighs, List<(int Index, decimal Price)> recentLows)
    {
        var current = candles[index];

        // Bullish CHoCH: price breaks above recent high after making lower lows (reversal from downtrend)
        if (recentHighs.Count >= 2 && recentLows.Count >= 2)
        {
            var lastHigh = recentHighs[0].Price;
            var wasLowerLows = recentLows[0].Price < recentLows[1].Price;

            if (current.Close > lastHigh && wasLowerLows)
            {
                return new StructureBreakDto(
                    Timestamp: current.Timestamp,
                    Type: StructureBreakType.CHoCH,
                    PreviousStructure: MarketStructure.Bearish,
                    NewStructure: MarketStructure.Bullish,
                    BreakPrice: current.Close,
                    PreviousHigh: lastHigh,
                    PreviousLow: recentLows[0].Price,
                    Description: "Bullish Change of Character - potential reversal to uptrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "reversalStrength", ((current.Close - recentLows[0].Price) / recentLows[0].Price) * 100m },
                        { "wasDowntrend", wasLowerLows }
                    }
                );
            }
        }

        // Bearish CHoCH: price breaks below recent low after making higher highs (reversal from uptrend)
        if (recentHighs.Count >= 2 && recentLows.Count >= 2)
        {
            var lastLow = recentLows[0].Price;
            var wasHigherHighs = recentHighs[0].Price > recentHighs[1].Price;

            if (current.Close < lastLow && wasHigherHighs)
            {
                return new StructureBreakDto(
                    Timestamp: current.Timestamp,
                    Type: StructureBreakType.CHoCH,
                    PreviousStructure: MarketStructure.Bullish,
                    NewStructure: MarketStructure.Bearish,
                    BreakPrice: current.Close,
                    PreviousHigh: recentHighs[0].Price,
                    PreviousLow: lastLow,
                    Description: "Bearish Change of Character - potential reversal to downtrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "reversalStrength", ((recentHighs[0].Price - current.Close) / recentHighs[0].Price) * 100m },
                        { "wasUptrend", wasHigherHighs }
                    }
                );
            }
        }

        return null;
    }

    private List<(int Index, decimal Price)> FindSwingHighs(IReadOnlyList<CandleDto> candles)
    {
        var swingHighs = new List<(int Index, decimal Price)>();
        int lookback = 5;

        for (int i = lookback; i < candles.Count - lookback; i++)
        {
            var current = candles[i];
            bool isSwingHigh = true;

            for (int j = i - lookback; j <= i + lookback; j++)
            {
                if (j != i && candles[j].High >= current.High)
                {
                    isSwingHigh = false;
                    break;
                }
            }

            if (isSwingHigh)
            {
                swingHighs.Add((i, current.High));
            }
        }

        return swingHighs;
    }

    private List<(int Index, decimal Price)> FindSwingLows(IReadOnlyList<CandleDto> candles)
    {
        var swingLows = new List<(int Index, decimal Price)>();
        int lookback = 5;

        for (int i = lookback; i < candles.Count - lookback; i++)
        {
            var current = candles[i];
            bool isSwingLow = true;

            for (int j = i - lookback; j <= i + lookback; j++)
            {
                if (j != i && candles[j].Low <= current.Low)
                {
                    isSwingLow = false;
                    break;
                }
            }

            if (isSwingLow)
            {
                swingLows.Add((i, current.Low));
            }
        }

        return swingLows;
    }

    private MarketStructure DetermineMarketStructure(IReadOnlyList<CandleDto> candles)
    {
        if (candles.Count < 20) return MarketStructure.Ranging;

        var recentCandles = candles.TakeLast(20).ToList();
        var swingHighs = FindSwingHighs(recentCandles);
        var swingLows = FindSwingLows(recentCandles);

        if (swingHighs.Count >= 2 && swingLows.Count >= 2)
        {
            // Check for higher highs and higher lows (bullish structure)
            bool hasHigherHighs = swingHighs[^1].Price > swingHighs[^2].Price;
            bool hasHigherLows = swingLows[^1].Price > swingLows[^2].Price;

            if (hasHigherHighs && hasHigherLows) return MarketStructure.Bullish;

            // Check for lower highs and lower lows (bearish structure)
            bool hasLowerHighs = swingHighs[^1].Price < swingHighs[^2].Price;
            bool hasLowerLows = swingLows[^1].Price < swingLows[^2].Price;

            if (hasLowerHighs && hasLowerLows) return MarketStructure.Bearish;
        }

        return MarketStructure.Ranging;
    }

    #endregion

    #region Premium/Discount Zones

    private PremiumDiscountZoneDto? CalculatePremiumDiscountZone(IReadOnlyList<CandleDto> candles)
    {
        if (candles.Count < 50) return null;

        var recentCandles = candles.TakeLast(50).ToList();
        var swingHigh = recentCandles.Max(c => c.High);
        var swingLow = recentCandles.Min(c => c.Low);
        var range = swingHigh - swingLow;
        var equilibrium = swingLow + (range / 2m);

        var currentPrice = candles.Last().Close;

        // Premium zone: 50-100% of range (sell zone)
        var premiumStart = equilibrium;
        var premiumEnd = swingHigh;

        // Discount zone: 0-50% of range (buy zone)
        var discountStart = swingLow;
        var discountEnd = equilibrium;

        ZoneType zoneType;
        decimal zoneHigh, zoneLow;
        string description;

        if (currentPrice >= equilibrium)
        {
            zoneType = ZoneType.Premium;
            zoneHigh = premiumEnd;
            zoneLow = premiumStart;
            var premiumPercent = ((currentPrice - equilibrium) / (premiumEnd - equilibrium)) * 100m;
            description = $"Price in Premium zone ({Math.Round(premiumPercent, 1)}%) - favorable for selling";
        }
        else
        {
            zoneType = ZoneType.Discount;
            zoneHigh = discountEnd;
            zoneLow = discountStart;
            var discountPercent = ((equilibrium - currentPrice) / (equilibrium - discountStart)) * 100m;
            description = $"Price in Discount zone ({Math.Round(discountPercent, 1)}%) - favorable for buying";
        }

        return new PremiumDiscountZoneDto(
            StartTimestamp: recentCandles.First().Timestamp,
            EndTimestamp: recentCandles.Last().Timestamp,
            Type: zoneType,
            HighPrice: zoneHigh,
            LowPrice: zoneLow,
            CurrentPrice: currentPrice,
            Description: description
        );
    }

    #endregion

    #region SMC Signals Generation

    private List<SmcSignalDto> GenerateSmcSignals(SmcAnalysisResponse analysis)
    {
        var signals = new List<SmcSignalDto>();

        // Signal from Order Blocks
        var unmitedOrderBlocks = analysis.OrderBlocks.Where(ob => !ob.IsMitigated && ob.Strength >= 70).ToList();
        foreach (var ob in unmitedOrderBlocks.Take(3))
        {
            if (ob.Type == OrderBlockType.Bullish && analysis.CurrentPrice <= ob.HighPrice * 1.01m)
            {
                signals.Add(new SmcSignalDto(
                    Symbol: analysis.Symbol,
                    Timeframe: analysis.Timeframe,
                    SignalType: "OrderBlock",
                    Direction: "LONG",
                    EntryPrice: ob.LowPrice,
                    StopLoss: ob.LowPrice * 0.98m,
                    TakeProfit: ob.LowPrice * 1.03m,
                    Confidence: ob.Strength,
                    Reason: $"Price near bullish order block (strength: {ob.Strength})",
                    Metadata: new Dictionary<string, object>
                    {
                        { "orderBlockHigh", ob.HighPrice },
                        { "orderBlockLow", ob.LowPrice }
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
            else if (ob.Type == OrderBlockType.Bearish && analysis.CurrentPrice >= ob.LowPrice * 0.99m)
            {
                signals.Add(new SmcSignalDto(
                    Symbol: analysis.Symbol,
                    Timeframe: analysis.Timeframe,
                    SignalType: "OrderBlock",
                    Direction: "SHORT",
                    EntryPrice: ob.HighPrice,
                    StopLoss: ob.HighPrice * 1.02m,
                    TakeProfit: ob.HighPrice * 0.97m,
                    Confidence: ob.Strength,
                    Reason: $"Price near bearish order block (strength: {ob.Strength})",
                    Metadata: new Dictionary<string, object>
                    {
                        { "orderBlockHigh", ob.HighPrice },
                        { "orderBlockLow", ob.LowPrice }
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        // Signal from Fair Value Gaps
        var unfilledFVGs = analysis.FairValueGaps.Where(fvg => !fvg.IsFilled).Take(3).ToList();
        foreach (var fvg in unfilledFVGs)
        {
            if (fvg.Type == OrderBlockType.Bullish && analysis.CurrentPrice <= fvg.TopPrice * 1.005m)
            {
                signals.Add(new SmcSignalDto(
                    Symbol: analysis.Symbol,
                    Timeframe: analysis.Timeframe,
                    SignalType: "FairValueGap",
                    Direction: "LONG",
                    EntryPrice: fvg.BottomPrice,
                    StopLoss: fvg.BottomPrice * 0.98m,
                    TakeProfit: fvg.TopPrice * 1.02m,
                    Confidence: 75m,
                    Reason: $"Price near bullish FVG (gap size: {Math.Round(fvg.GapSize, 2)})",
                    Metadata: new Dictionary<string, object>
                    {
                        { "fvgTop", fvg.TopPrice },
                        { "fvgBottom", fvg.BottomPrice },
                        { "gapSize", fvg.GapSize }
                    },
                    Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                ));
            }
        }

        // Signal from Market Structure
        var recentStructureBreak = analysis.StructureBreaks.FirstOrDefault();
        if (recentStructureBreak != null)
        {
            if (recentStructureBreak.Type == StructureBreakType.CHoCH && recentStructureBreak.NewStructure == MarketStructure.Bullish)
            {
                signals.Add(new SmcSignalDto(
                    Symbol: analysis.Symbol,
                    Timeframe: analysis.Timeframe,
                    SignalType: "StructureBreak",
                    Direction: "LONG",
                    EntryPrice: analysis.CurrentPrice,
                    StopLoss: recentStructureBreak.PreviousLow,
                    TakeProfit: analysis.CurrentPrice * 1.03m,
                    Confidence: 80m,
                    Reason: "Bullish Change of Character detected - potential reversal to uptrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "breakType", "CHoCH" },
                        { "breakPrice", recentStructureBreak.BreakPrice }
                    },
                    Timestamp: recentStructureBreak.Timestamp
                ));
            }
            else if (recentStructureBreak.Type == StructureBreakType.CHoCH && recentStructureBreak.NewStructure == MarketStructure.Bearish)
            {
                signals.Add(new SmcSignalDto(
                    Symbol: analysis.Symbol,
                    Timeframe: analysis.Timeframe,
                    SignalType: "StructureBreak",
                    Direction: "SHORT",
                    EntryPrice: analysis.CurrentPrice,
                    StopLoss: recentStructureBreak.PreviousHigh,
                    TakeProfit: analysis.CurrentPrice * 0.97m,
                    Confidence: 80m,
                    Reason: "Bearish Change of Character detected - potential reversal to downtrend",
                    Metadata: new Dictionary<string, object>
                    {
                        { "breakType", "CHoCH" },
                        { "breakPrice", recentStructureBreak.BreakPrice }
                    },
                    Timestamp: recentStructureBreak.Timestamp
                ));
            }
        }

        return signals;
    }

    #endregion

    #region Market Bias

    public async Task<MarketBiasDto> GetMarketBiasAsync(string symbol, string timeframe)
    {
        try
        {
            var analysis = await GetSmcAnalysisAsync(symbol, timeframe);
            var indicators = await _marketDataService.GetCandlesAsync(symbol, timeframe, 50);

            var currentPrice = analysis.CurrentPrice;
            var structure = analysis.CurrentStructure;
            var reasons = new List<string>();

            // Determine bias based on market structure
            string direction = "NEUTRAL";
            int strength = 0;

            switch (structure)
            {
                case MarketStructure.Bullish:
                    direction = "BULLISH";
                    strength = 70;
                    reasons.Add("Market structure is bullish (higher highs, higher lows)");
                    break;
                case MarketStructure.Bearish:
                    direction = "BEARISH";
                    strength = 70;
                    reasons.Add("Market structure is bearish (lower highs, lower lows)");
                    break;
                case MarketStructure.Ranging:
                    direction = "NEUTRAL";
                    strength = 30;
                    reasons.Add("Market is ranging (no clear structure)");
                    break;
            }

            // Check premium/discount zone
            if (analysis.PremiumDiscountZone != null)
            {
                var zone = analysis.PremiumDiscountZone;
                if (zone.Type == ZoneType.Premium)
                {
                    reasons.Add("Price in Premium zone - favorable for selling");
                    if (direction == "BULLISH") strength -= 15;
                    else if (direction == "BEARISH") strength += 15;
                }
                else if (zone.Type == ZoneType.Discount)
                {
                    reasons.Add("Price in Discount zone - favorable for buying");
                    if (direction == "BULLISH") strength += 15;
                    else if (direction == "BEARISH") strength -= 15;
                }
            }

            // Check for active order blocks near current price
            var activeOrderBlocks = analysis.OrderBlocks.Where(ob => !ob.IsMitigated && ob.Strength >= 60).ToList();
            if (activeOrderBlocks.Any())
            {
                var bullishOB = activeOrderBlocks.FirstOrDefault(ob => ob.Type == OrderBlockType.Bullish);
                var bearishOB = activeOrderBlocks.FirstOrDefault(ob => ob.Type == OrderBlockType.Bearish);

                if (bullishOB != null && currentPrice <= bullishOB.HighPrice * 1.02m)
                {
                    direction = "BULLISH";
                    strength = Math.Min(90, strength + 20);
                    reasons.Add($"Price near bullish order block (strength: {bullishOB.Strength})");
                }
                else if (bearishOB != null && currentPrice >= bearishOB.LowPrice * 0.98m)
                {
                    direction = "BEARISH";
                    strength = Math.Min(90, strength + 20);
                    reasons.Add($"Price near bearish order block (strength: {bearishOB.Strength})");
                }
            }

            // Check for unfilled FVGs
            var unfilledFVGs = analysis.FairValueGaps.Where(fvg => !fvg.IsFilled).ToList();
            if (unfilledFVGs.Any())
            {
                var bullishFVG = unfilledFVGs.FirstOrDefault(fvg => fvg.Type == OrderBlockType.Bullish);
                var bearishFVG = unfilledFVGs.FirstOrDefault(fvg => fvg.Type == OrderBlockType.Bearish);

                if (bullishFVG != null && currentPrice <= bullishFVG.TopPrice * 1.01m)
                {
                    if (direction != "BEARISH") direction = "BULLISH";
                    strength = Math.Min(85, strength + 15);
                    reasons.Add($"Bullish FVG nearby (gap: {Math.Round(bullishFVG.GapSize, 2)})");
                }
                else if (bearishFVG != null && currentPrice >= bearishFVG.BottomPrice * 0.99m)
                {
                    if (direction != "BULLISH") direction = "BEARISH";
                    strength = Math.Min(85, strength + 15);
                    reasons.Add($"Bearish FVG nearby (gap: {Math.Round(bearishFVG.GapSize, 2)})");
                }
            }

            // Check structure breaks
            var recentBreaks = analysis.StructureBreaks.Take(2).ToList();
            foreach (var sb in recentBreaks)
            {
                if (sb.Type == StructureBreakType.CHoCH)
                {
                    if (sb.NewStructure == MarketStructure.Bullish)
                    {
                        direction = "BULLISH";
                        strength = Math.Min(85, strength + 15);
                        reasons.Add("Bullish Change of Character detected");
                    }
                    else if (sb.NewStructure == MarketStructure.Bearish)
                    {
                        direction = "BEARISH";
                        strength = Math.Min(85, strength + 15);
                        reasons.Add("Bearish Change of Character detected");
                    }
                }
            }

            // Clamp strength
            strength = Math.Clamp(strength, 0, 100);

            return new MarketBiasDto
            {
                Direction = direction,
                Strength = strength,
                Reasons = reasons.ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market bias for {Symbol} {Timeframe}", symbol, timeframe);
            return new MarketBiasDto
            {
                Direction = "NEUTRAL",
                Strength = 0,
                Reasons = new[] { "Error calculating bias" }
            };
        }
    }

    #endregion
}
