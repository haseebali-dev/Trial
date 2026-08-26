using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/smc")]
public class SmartMoneyConceptsController : ControllerBase
{
    private readonly ISmartMoneyConceptsService _smcService;

    public SmartMoneyConceptsController(ISmartMoneyConceptsService smcService)
    {
        _smcService = smcService;
    }

    [HttpGet("{symbol}/{timeframe}")]
    public async Task<ActionResult<SmcAnalysisResponse>> GetSmcAnalysis(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var analysis = await _smcService.GetSmcAnalysisAsync(symbol, timeframe, limit);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SMC_UNAVAILABLE" });
        }
    }

    [HttpGet("{symbol}/{timeframe}/orderblocks")]
    public async Task<ActionResult<IReadOnlyList<OrderBlockDto>>> GetOrderBlocks(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var orderBlocks = await _smcService.GetOrderBlocksAsync(symbol, timeframe, limit);
            return Ok(orderBlocks);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/fvg")]
    public async Task<ActionResult<IReadOnlyList<FairValueGapDto>>> GetFairValueGaps(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var fvgs = await _smcService.GetFairValueGapsAsync(symbol, timeframe, limit);
            return Ok(fvgs);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/liquidity")]
    public async Task<ActionResult<IReadOnlyList<LiquidityZoneDto>>> GetLiquidityZones(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var liquidityZones = await _smcService.GetLiquidityZonesAsync(symbol, timeframe, limit);
            return Ok(liquidityZones);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/structure")]
    public async Task<ActionResult<IReadOnlyList<StructureBreakDto>>> GetStructureBreaks(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var structureBreaks = await _smcService.GetStructureBreaksAsync(symbol, timeframe, limit);
            return Ok(structureBreaks);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/signals")]
    public async Task<ActionResult<IReadOnlyList<SmcSignalDto>>> GetSmcSignals(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var signals = await _smcService.GetSmcSignalsAsync(symbol, timeframe, limit);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/bias")]
    public async Task<ActionResult<MarketBiasDto>> GetMarketBias(
        string symbol,
        string timeframe)
    {
        try
        {
            var bias = await _smcService.GetMarketBiasAsync(symbol, timeframe);
            return Ok(bias);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}
