using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/indicators")]
public class TechnicalAnalysisController : ControllerBase
{
    private readonly ITechnicalAnalysisService _technicalAnalysisService;

    public TechnicalAnalysisController(ITechnicalAnalysisService technicalAnalysisService)
    {
        _technicalAnalysisService = technicalAnalysisService;
    }

    [HttpGet("{symbol}/{timeframe}")]
    public async Task<ActionResult<IndicatorsResponse>> GetAllIndicators(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var indicators = await _technicalAnalysisService.GetIndicatorsAsync(symbol, timeframe, limit);
            return Ok(indicators);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "INDICATORS_UNAVAILABLE" });
        }
    }

    [HttpGet("{symbol}/{timeframe}/sma/{period}")]
    public async Task<ActionResult<IReadOnlyList<SmaDto>>> GetSma(
        string symbol,
        string timeframe,
        int period,
        [FromQuery] int limit = 500)
    {
        try
        {
            var sma = await _technicalAnalysisService.GetSmaAsync(symbol, timeframe, period, limit);
            return Ok(sma);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/ema/{period}")]
    public async Task<ActionResult<IReadOnlyList<EmaDto>>> GetEma(
        string symbol,
        string timeframe,
        int period,
        [FromQuery] int limit = 500)
    {
        try
        {
            var ema = await _technicalAnalysisService.GetEmaAsync(symbol, timeframe, period, limit);
            return Ok(ema);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/rsi")]
    public async Task<ActionResult<IReadOnlyList<RsiDto>>> GetRsi(
        string symbol,
        string timeframe,
        [FromQuery] int period = 14,
        [FromQuery] int limit = 500)
    {
        try
        {
            var rsi = await _technicalAnalysisService.GetRsiAsync(symbol, timeframe, period, limit);
            return Ok(rsi);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/macd")]
    public async Task<ActionResult<IReadOnlyList<MacdDto>>> GetMacd(
        string symbol,
        string timeframe,
        [FromQuery] int fastPeriod = 12,
        [FromQuery] int slowPeriod = 26,
        [FromQuery] int signalPeriod = 9,
        [FromQuery] int limit = 500)
    {
        try
        {
            var macd = await _technicalAnalysisService.GetMacdAsync(symbol, timeframe, fastPeriod, slowPeriod, signalPeriod, limit);
            return Ok(macd);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/bollinger")]
    public async Task<ActionResult<IReadOnlyList<BollingerBandsDto>>> GetBollingerBands(
        string symbol,
        string timeframe,
        [FromQuery] int period = 20,
        [FromQuery] decimal stdDev = 2,
        [FromQuery] int limit = 500)
    {
        try
        {
            var bb = await _technicalAnalysisService.GetBollingerBandsAsync(symbol, timeframe, period, stdDev, limit);
            return Ok(bb);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/atr")]
    public async Task<ActionResult<IReadOnlyList<AtrDto>>> GetAtr(
        string symbol,
        string timeframe,
        [FromQuery] int period = 14,
        [FromQuery] int limit = 500)
    {
        try
        {
            var atr = await _technicalAnalysisService.GetAtrAsync(symbol, timeframe, period, limit);
            return Ok(atr);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/stochastic")]
    public async Task<ActionResult<IReadOnlyList<StochasticDto>>> GetStochastic(
        string symbol,
        string timeframe,
        [FromQuery] int kPeriod = 14,
        [FromQuery] int dPeriod = 3,
        [FromQuery] int limit = 500)
    {
        try
        {
            var stoch = await _technicalAnalysisService.GetStochasticAsync(symbol, timeframe, kPeriod, dPeriod, limit);
            return Ok(stoch);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/adx")]
    public async Task<ActionResult<IReadOnlyList<AdxDto>>> GetAdx(
        string symbol,
        string timeframe,
        [FromQuery] int period = 14,
        [FromQuery] int limit = 500)
    {
        try
        {
            var adx = await _technicalAnalysisService.GetAdxAsync(symbol, timeframe, period, limit);
            return Ok(adx);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/obv")]
    public async Task<ActionResult<IReadOnlyList<ObvDto>>> GetObv(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var obv = await _technicalAnalysisService.GetObvAsync(symbol, timeframe, limit);
            return Ok(obv);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/vwap")]
    public async Task<ActionResult<IReadOnlyList<VwapDto>>> GetVwap(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var vwap = await _technicalAnalysisService.GetVwapAsync(symbol, timeframe, limit);
            return Ok(vwap);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}
