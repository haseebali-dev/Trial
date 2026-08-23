using CryptoTrading.API.DTOs;
using CryptoTrading.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.API.Controllers;

[ApiController]
[Route("api/scanner")]
public class ScannerController : ControllerBase
{
    private readonly IScannerService _scannerService;

    public ScannerController(IScannerService scannerService)
    {
        _scannerService = scannerService;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ScannerSummaryDto>> ScanMarket([FromBody] ScanRequestDto request)
    {
        try
        {
            var summary = await _scannerService.ScanMarketAsync(request);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SCANNER_UNAVAILABLE" });
        }
    }

    [HttpGet("scan")]
    public async Task<ActionResult<ScannerSummaryDto>> ScanMarketGet(
        [FromQuery] string[]? symbols = null,
        [FromQuery] string[]? timeframes = null,
        [FromQuery] decimal minimumScore = 5,
        [FromQuery] int maxConcurrentScans = 5)
    {
        try
        {
            var request = new ScanRequestDto
            {
                Symbols = symbols?.ToList(),
                Timeframes = timeframes?.ToList(),
                MinimumScore = minimumScore,
                MaxConcurrentScans = maxConcurrentScans
            };

            var summary = await _scannerService.ScanMarketAsync(request);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SCANNER_UNAVAILABLE" });
        }
    }

    [HttpGet("symbol/{symbol}/{timeframe}")]
    public async Task<ActionResult<SymbolScanDetailsDto>> ScanSymbol(string symbol, string timeframe)
    {
        try
        {
            var result = await _scannerService.ScanSymbolAsync(symbol.ToUpper(), timeframe.ToLower());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SCANNER_UNAVAILABLE" });
        }
    }

    [HttpPost("symbols")]
    public async Task<ActionResult<IReadOnlyList<ScanResultDto>>> ScanSymbols([FromBody] ScanSymbolsRequestDto request)
    {
        try
        {
            var results = await _scannerService.ScanSymbolsAsync(request.Symbols, request.Timeframe, request.MinimumScore);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message, code = "SCANNER_UNAVAILABLE" });
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<ScannerStatusDto>> GetScannerStatus()
    {
        try
        {
            var isScanning = await _scannerService.IsScanningAsync();
            return Ok(new ScannerStatusDto(isScanning));
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}

public record ScanSymbolsRequestDto(
    List<string> Symbols,
    string Timeframe,
    decimal MinimumScore = 5
);

public record ScannerStatusDto(bool IsScanning);