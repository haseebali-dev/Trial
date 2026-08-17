namespace CryptoTrading.API.DTOs;

public record MarketTickerDto(
    string Symbol,
    decimal Price,
    decimal Volume24h,
    decimal Change24h,
    long Timestamp,
    decimal? High24h = null,
    decimal? Low24h = null,
    decimal? Open24h = null
);

public record CandleDto(
    long Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume
);

public record MarketOverviewDto(
    string Symbol,
    decimal Price,
    decimal Change24h,
    decimal Volume24h
);

public record GetCandlesRequest(
    string Symbol,
    string Timeframe,
    int Limit = 500
);

public record GetTickerRequest(string Symbol);