using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/market")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketDataController(IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("ticker/{symbol}")]
    public async Task<ActionResult<MarketTickerDto>> GetTicker(string symbol)
    {
        try
        {
            var ticker = await _marketDataService.GetTickerAsync(symbol.ToUpper());
            return Ok(ticker);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, code = "INVALID_SYMBOL" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "MARKET_DATA_UNAVAILABLE" });
        }
    }

    [HttpGet("candles/{symbol}/{timeframe}")]
    public async Task<ActionResult<IReadOnlyList<CandleDto>>> GetCandles(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var validTimeframes = new[] { "1m", "5m", "15m", "30m", "1h", "4h", "1d" };
            if (!validTimeframes.Contains(timeframe.ToLower()))
            {
                return BadRequest(new { success = false, message = "Invalid timeframe", code = "INVALID_TIMEFRAME" });
            }

            limit = Math.Clamp(limit, 1, 1000);
            var candles = await _marketDataService.GetCandlesAsync(symbol.ToUpper(), timeframe.ToLower(), limit);
            return Ok(candles);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, code = "INVALID_REQUEST" });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "MARKET_DATA_UNAVAILABLE" });
        }
    }

    [HttpGet("overview")]
    public async Task<ActionResult<IReadOnlyList<MarketOverviewDto>>> GetMarketOverview()
    {
        try
        {
            var overview = await _marketDataService.GetMarketOverviewAsync();
            return Ok(overview);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "MARKET_DATA_UNAVAILABLE" });
        }
    }

    [HttpGet("health")]
    public async Task<ActionResult> GetHealth()
    {
        var healthy = await _marketDataService.IsHealthyAsync();
        return healthy ? Ok(new { status = "healthy" }) : StatusCode(503, new { status = "unhealthy" });
    }
}