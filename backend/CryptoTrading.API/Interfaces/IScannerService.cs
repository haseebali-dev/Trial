using CryptoTrading.API.DTOs;

namespace CryptoTrading.API.Interfaces;

public interface IScannerService
{
    Task<ScannerSummaryDto> ScanMarketAsync(ScanRequestDto request);
    Task<SymbolScanDetailsDto> ScanSymbolAsync(string symbol, string timeframe);
    Task<IReadOnlyList<ScanResultDto>> ScanSymbolsAsync(List<string> symbols, string timeframe, decimal minimumScore = 5);
    Task<bool> IsScanningAsync();
}