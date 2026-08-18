using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/signals")]
public class SignalController : ControllerBase
{
    private readonly ISignalGenerationService _signalGenerationService;

    public SignalController(ISignalGenerationService signalGenerationService)
    {
        _signalGenerationService = signalGenerationService;
    }

    [HttpGet("{symbol}/{timeframe}")]
    public async Task<ActionResult<SignalSummaryDto>> GetSignals(
        string symbol,
        string timeframe,
        [FromQuery] string[]? strategies = null)
    {
        try
        {
            var signals = await _signalGenerationService.GetSignalsAsync(symbol, timeframe, strategies);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SIGNALS_UNAVAILABLE" });
        }
    }

    [HttpGet("{symbol}/{timeframe}/trend")]
    public async Task<ActionResult<IReadOnlyList<SignalDto>>> GetTrendFollowingSignals(
        string symbol,
        string timeframe)
    {
        try
        {
            var signals = await _signalGenerationService.GetTrendFollowingSignalsAsync(symbol, timeframe);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/meanreversion")]
    public async Task<ActionResult<IReadOnlyList<SignalDto>>> GetMeanReversionSignals(
        string symbol,
        string timeframe)
    {
        try
        {
            var signals = await _signalGenerationService.GetMeanReversionSignalsAsync(symbol, timeframe);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/breakout")]
    public async Task<ActionResult<IReadOnlyList<SignalDto>>> GetBreakoutSignals(
        string symbol,
        string timeframe)
    {
        try
        {
            var signals = await _signalGenerationService.GetBreakoutSignalsAsync(symbol, timeframe);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/momentum")]
    public async Task<ActionResult<IReadOnlyList<SignalDto>>> GetMomentumSignals(
        string symbol,
        string timeframe)
    {
        try
        {
            var signals = await _signalGenerationService.GetMomentumSignalsAsync(symbol, timeframe);
            return Ok(signals);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}
