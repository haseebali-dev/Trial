using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/patterns")]
public class PatternRecognitionController : ControllerBase
{
    private readonly IPatternRecognitionService _patternRecognitionService;

    public PatternRecognitionController(IPatternRecognitionService patternRecognitionService)
    {
        _patternRecognitionService = patternRecognitionService;
    }

    [HttpGet("{symbol}/{timeframe}")]
    public async Task<ActionResult<PatternRecognitionResponse>> GetAllPatterns(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var patterns = await _patternRecognitionService.GetPatternsAsync(symbol, timeframe, limit);
            return Ok(patterns);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "PATTERNS_UNAVAILABLE" });
        }
    }

    [HttpGet("{symbol}/{timeframe}/candles")]
    public async Task<ActionResult<IReadOnlyList<CandlePatternDto>>> GetCandlePatterns(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var patterns = await _patternRecognitionService.GetCandlePatternsAsync(symbol, timeframe, limit);
            return Ok(patterns);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/charts")]
    public async Task<ActionResult<IReadOnlyList<ChartPatternDto>>> GetChartPatterns(
        string symbol,
        string timeframe,
        [FromQuery] int limit = 500)
    {
        try
        {
            var patterns = await _patternRecognitionService.GetChartPatternsAsync(symbol, timeframe, limit);
            return Ok(patterns);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{symbol}/{timeframe}/alerts")]
    public async Task<ActionResult<IReadOnlyList<PatternAlertDto>>> GetRecentAlerts(
        string symbol,
        string timeframe,
        [FromQuery] int hours = 24)
    {
        try
        {
            var alerts = await _patternRecognitionService.GetRecentAlertsAsync(symbol, timeframe, hours);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}
