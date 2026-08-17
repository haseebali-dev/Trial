using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.API.Services;

public class TechnicalAnalysisService : ITechnicalAnalysisService
{
    private readonly IMarketDataService _marketDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TechnicalAnalysisService> _logger;

    public TechnicalAnalysisService(
        IMarketDataService marketDataService,
        IMemoryCache cache,
        ILogger<TechnicalAnalysisService> logger)
    {
        _marketDataService = marketDataService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IndicatorsResponse> GetIndicatorsAsync(string symbol, string timeframe, int limit = 500, string[]? indicators = null)
    {
        var cacheKey = $"indicators:{symbol}:{timeframe}:{limit}";
        if (_cache.TryGetValue(cacheKey, out IndicatorsResponse? cached))
        {
            return cached!;
        }

        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);

        var response = new IndicatorsResponse(
            Symbol: symbol,
            Timeframe: timeframe,
            Sma20: CalculateSma(candles, 20),
            Sma50: CalculateSma(candles, 50),
            Sma200: CalculateSma(candles, 200),
            Ema9: CalculateEma(candles, 9),
            Ema21: CalculateEma(candles, 21),
            Ema50: CalculateEma(candles, 50),
            Rsi14: CalculateRsi(candles, 14),
            Macd: CalculateMacd(candles, 12, 26, 9),
            BollingerBands20: CalculateBollingerBands(candles, 20, 2),
            Atr14: CalculateAtr(candles, 14),
            Stochastic14: CalculateStochastic(candles, 14, 3),
            Adx14: CalculateAdx(candles, 14),
            Obv: CalculateObv(candles),
            Vwap: CalculateVwap(candles)
        );

        _cache.Set(cacheKey, response, TimeSpan.FromSeconds(30));
        return response;
    }

    public async Task<IReadOnlyList<SmaDto>> GetSmaAsync(string symbol, string timeframe, int period, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateSma(candles, period);
    }

    public async Task<IReadOnlyList<EmaDto>> GetEmaAsync(string symbol, string timeframe, int period, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateEma(candles, period);
    }

    public async Task<IReadOnlyList<RsiDto>> GetRsiAsync(string symbol, string timeframe, int period = 14, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateRsi(candles, period);
    }

    public async Task<IReadOnlyList<MacdDto>> GetMacdAsync(string symbol, string timeframe, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateMacd(candles, fastPeriod, slowPeriod, signalPeriod);
    }

    public async Task<IReadOnlyList<BollingerBandsDto>> GetBollingerBandsAsync(string symbol, string timeframe, int period = 20, decimal stdDev = 2, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateBollingerBands(candles, period, stdDev);
    }

    public async Task<IReadOnlyList<AtrDto>> GetAtrAsync(string symbol, string timeframe, int period = 14, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateAtr(candles, period);
    }

    public async Task<IReadOnlyList<StochasticDto>> GetStochasticAsync(string symbol, string timeframe, int kPeriod = 14, int dPeriod = 3, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateStochastic(candles, kPeriod, dPeriod);
    }

    public async Task<IReadOnlyList<AdxDto>> GetAdxAsync(string symbol, string timeframe, int period = 14, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateAdx(candles, period);
    }

    public async Task<IReadOnlyList<ObvDto>> GetObvAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateObv(candles);
    }

    public async Task<IReadOnlyList<VwapDto>> GetVwapAsync(string symbol, string timeframe, int limit = 500)
    {
        var candles = await _marketDataService.GetCandlesAsync(symbol, timeframe, limit);
        return CalculateVwap(candles);
    }

    private List<SmaDto> CalculateSma(IReadOnlyList<CandleDto> candles, int period)
    {
        var result = new List<SmaDto>();
        if (candles.Count < period) return result;

        for (int i = period - 1; i < candles.Count; i++)
        {
            var sum = 0m;
            for (int j = 0; j < period; j++)
            {
                sum += candles[i - j].Close;
            }
            result.Add(new SmaDto(candles[i].Timestamp, sum / period));
        }
        return result;
    }

    private List<EmaDto> CalculateEma(IReadOnlyList<CandleDto> candles, int period)
    {
        var result = new List<EmaDto>();
        if (candles.Count < period) return result;

        var multiplier = 2m / (period + 1);

        // First EMA is SMA
        var sum = 0m;
        for (int i = 0; i < period; i++)
        {
            sum += candles[i].Close;
        }
        var ema = sum / period;
        result.Add(new EmaDto(candles[period - 1].Timestamp, ema));

        // Calculate EMA for remaining candles
        for (int i = period; i < candles.Count; i++)
        {
            ema = (candles[i].Close - ema) * multiplier + ema;
            result.Add(new EmaDto(candles[i].Timestamp, ema));
        }
        return result;
    }

    private List<RsiDto> CalculateRsi(IReadOnlyList<CandleDto> candles, int period)
    {
        var result = new List<RsiDto>();
        if (candles.Count < period + 1) return result;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? -change : 0);
        }

        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        for (int i = period; i < gains.Count; i++)
        {
            avgGain = (avgGain * (period - 1) + gains[i]) / period;
            avgLoss = (avgLoss * (period - 1) + losses[i]) / period;

            var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            var rsi = 100 - (100 / (1 + rs));
            result.Add(new RsiDto(candles[i + 1].Timestamp, rsi));
        }
        return result;
    }

    private List<MacdDto> CalculateMacd(IReadOnlyList<CandleDto> candles, int fastPeriod, int slowPeriod, int signalPeriod)
    {
        var result = new List<MacdDto>();
        if (candles.Count < slowPeriod + signalPeriod) return result;

        var fastEma = CalculateEma(candles, fastPeriod);
        var slowEma = CalculateEma(candles, slowPeriod);

        var macdLine = new List<(long Timestamp, decimal Value)>();
        var startIndex = slowPeriod - fastPeriod;

        for (int i = 0; i < slowEma.Count; i++)
        {
            var macd = fastEma[i + startIndex].Value - slowEma[i].Value;
            macdLine.Add((slowEma[i].Timestamp, macd));
        }

        // Calculate signal line (EMA of MACD)
        var multiplier = 2m / (signalPeriod + 1);
        var signalEma = macdLine.Take(signalPeriod).Average(x => x.Value);

        for (int i = signalPeriod - 1; i < macdLine.Count; i++)
        {
            if (i > signalPeriod - 1)
            {
                signalEma = (macdLine[i].Value - signalEma) * multiplier + signalEma;
            }

            var histogram = macdLine[i].Value - signalEma;
            result.Add(new MacdDto(macdLine[i].Timestamp, macdLine[i].Value, signalEma, histogram));
        }
        return result;
    }

    private List<BollingerBandsDto> CalculateBollingerBands(IReadOnlyList<CandleDto> candles, int period, decimal stdDev)
    {
        var result = new List<BollingerBandsDto>();
        if (candles.Count < period) return result;

        var sma = CalculateSma(candles, period);

        for (int i = 0; i < sma.Count; i++)
        {
            var candleIndex = i + period - 1;
            var prices = new List<decimal>();
            for (int j = 0; j < period; j++)
            {
                prices.Add(candles[candleIndex - j].Close);
            }

            var mean = sma[i].Value;
            var variance = prices.Sum(p => (p - mean) * (p - mean)) / period;
            var sd = (decimal)Math.Sqrt((double)variance);

            var upper = mean + (stdDev * sd);
            var lower = mean - (stdDev * sd);
            var percentB = sd == 0 ? 0.5m : (candles[candleIndex].Close - lower) / (upper - lower);
            var bandwidth = sd == 0 ? 0 : (upper - lower) / mean;

            result.Add(new BollingerBandsDto(candles[candleIndex].Timestamp, upper, mean, lower, percentB, bandwidth));
        }
        return result;
    }

    private List<AtrDto> CalculateAtr(IReadOnlyList<CandleDto> candles, int period)
    {
        var result = new List<AtrDto>();
        if (candles.Count < period + 1) return result;

        var trueRanges = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            var high = candles[i].High;
            var low = candles[i].Low;
            var prevClose = candles[i - 1].Close;

            var tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
            trueRanges.Add(tr);
        }

        var atr = trueRanges.Take(period).Average();
        result.Add(new AtrDto(candles[period].Timestamp, atr));

        for (int i = period; i < trueRanges.Count; i++)
        {
            atr = (atr * (period - 1) + trueRanges[i]) / period;
            result.Add(new AtrDto(candles[i + 1].Timestamp, atr));
        }
        return result;
    }

    private List<StochasticDto> CalculateStochastic(IReadOnlyList<CandleDto> candles, int kPeriod, int dPeriod)
    {
        var result = new List<StochasticDto>();
        if (candles.Count < kPeriod + dPeriod) return result;

        var kValues = new List<(long Timestamp, decimal K)>();

        for (int i = kPeriod - 1; i < candles.Count; i++)
        {
            var high = candles.Skip(i - kPeriod + 1).Take(kPeriod).Max(c => c.High);
            var low = candles.Skip(i - kPeriod + 1).Take(kPeriod).Min(c => c.Low);
            var close = candles[i].Close;

            var k = high == low ? 50m : ((close - low) / (high - low)) * 100;
            kValues.Add((candles[i].Timestamp, k));
        }

        for (int i = dPeriod - 1; i < kValues.Count; i++)
        {
            var d = kValues.Skip(i - dPeriod + 1).Take(dPeriod).Average(x => x.K);
            result.Add(new StochasticDto(kValues[i].Timestamp, kValues[i].K, d));
        }
        return result;
    }

    private List<AdxDto> CalculateAdx(IReadOnlyList<CandleDto> candles, int period)
    {
        var result = new List<AdxDto>();
        if (candles.Count < period * 2) return result;

        var plusDm = new List<decimal>();
        var minusDm = new List<decimal>();
        var tr = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var highDiff = candles[i].High - candles[i - 1].High;
            var lowDiff = candles[i - 1].Low - candles[i].Low;

            plusDm.Add(highDiff > lowDiff && highDiff > 0 ? highDiff : 0);
            minusDm.Add(lowDiff > highDiff && lowDiff > 0 ? lowDiff : 0);

            var high = candles[i].High;
            var low = candles[i].Low;
            var prevClose = candles[i - 1].Close;
            tr.Add(Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose))));
        }

        var smoothedPlusDm = plusDm.Take(period).Sum();
        var smoothedMinusDm = minusDm.Take(period).Sum();
        var smoothedTr = tr.Take(period).Sum();

        for (int i = period; i < plusDm.Count; i++)
        {
            smoothedPlusDm = smoothedPlusDm - (smoothedPlusDm / period) + plusDm[i];
            smoothedMinusDm = smoothedMinusDm - (smoothedMinusDm / period) + minusDm[i];
            smoothedTr = smoothedTr - (smoothedTr / period) + tr[i];

            var plusDi = smoothedTr == 0 ? 0 : (smoothedPlusDm / smoothedTr) * 100;
            var minusDi = smoothedTr == 0 ? 0 : (smoothedMinusDm / smoothedTr) * 100;
            var dx = (plusDi + minusDi) == 0 ? 0 : Math.Abs(plusDi - minusDi) / (plusDi + minusDi) * 100;

            if (i >= period * 2 - 1)
            {
                var adx = result.Count == 0
                    ? dx
                    : (result.Last().Adx * (period - 1) + dx) / period;

                result.Add(new AdxDto(candles[i + 1].Timestamp, adx, plusDi, minusDi));
            }
        }
        return result;
    }

    private List<ObvDto> CalculateObv(IReadOnlyList<CandleDto> candles)
    {
        var result = new List<ObvDto>();
        if (candles.Count < 2) return result;

        var obv = 0m;
        result.Add(new ObvDto(candles[0].Timestamp, obv));

        for (int i = 1; i < candles.Count; i++)
        {
            if (candles[i].Close > candles[i - 1].Close)
                obv += candles[i].Volume;
            else if (candles[i].Close < candles[i - 1].Close)
                obv -= candles[i].Volume;

            result.Add(new ObvDto(candles[i].Timestamp, obv));
        }
        return result;
    }

    private List<VwapDto> CalculateVwap(IReadOnlyList<CandleDto> candles)
    {
        var result = new List<VwapDto>();
        if (candles.Count == 0) return result;

        var cumulativeTPV = 0m; // Typical Price * Volume
        var cumulativeVolume = 0m;

        for (int i = 0; i < candles.Count; i++)
        {
            var typicalPrice = (candles[i].High + candles[i].Low + candles[i].Close) / 3;
            cumulativeTPV += typicalPrice * candles[i].Volume;
            cumulativeVolume += candles[i].Volume;

            var vwap = cumulativeVolume == 0 ? typicalPrice : cumulativeTPV / cumulativeVolume;
            result.Add(new VwapDto(candles[i].Timestamp, vwap));
        }
        return result;
    }
}
