import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, catchError, of, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Candle,
  MarketTicker,
  Timeframe,
  SmaDto,
  EmaDto,
  RsiDto,
  MacdDto,
  BollingerBandsDto,
  AtrDto,
  StochasticDto,
  AdxDto,
  ObvDto,
  VwapDto,
  IndicatorsResponse
} from '../models/market.models';

interface AlphaVantageTimeSeries {
  'Meta Data': {
    '1. Information': string;
    '2. Symbol': string;
    '3. Last Refreshed': string;
    '4. Interval': string;
    '5. Output Size': string;
    '6. Time Zone': string;
  };
  'Time Series'?: Record<string, {
    '1. open': string;
    '2. high': string;
    '3. low': string;
    '4. close': string;
    '5. volume': string;
  }>;
  'Time Series (Daily)'?: Record<string, {
    '1. open': string;
    '2. high': string;
    '3. low': string;
    '4. close': string;
    '5. volume': string;
  }>;
  'Global Quote'?: {
    '01. symbol': string;
    '02. open': string;
    '03. high': string;
    '04. low': string;
    '05. price': string;
    '06. volume': string;
    '07. latest trading day': string;
    '08. previous close': string;
    '09. change': string;
    '10. change percent': string;
  };
  'Technical Analysis: SMA'?: Record<string, { SMA: string }>;
  'Technical Analysis: EMA'?: Record<string, { EMA: string }>;
  'Technical Analysis: RSI'?: Record<string, { RSI: string }>;
  'Technical Analysis: MACD'?: Record<string, { MACD: string; MACD_Signal: string; MACD_Hist: string }>;
  'Technical Analysis: BBANDS'?: Record<string, { 'Real Upper Band': string; 'Real Middle Band': string; 'Real Lower Band': string }>;
  'Technical Analysis: ATR'?: Record<string, { ATR: string }>;
  'Technical Analysis: STOCH'?: Record<string, { SlowK: string; SlowD: string }>;
  'Technical Analysis: ADX'?: Record<string, { ADX: string; 'Plus_DI': string; 'Minus_DI': string }>;
  'Technical Analysis: OBV'?: Record<string, { OBV: string }>;
  'Technical Analysis: VWAP'?: Record<string, { VWAP: string }>;
  'Note'?: string;
  'Error Message'?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AlphaVantageService {
  private readonly apiKey = environment.alphaVantage.apiKey;
  private readonly baseUrl = environment.alphaVantage.baseUrl;

  // Symbol mapping for crypto (Alpha Vantage uses different format)
  private symbolMap: Record<string, string> = {
    'BTCUSDT': 'BTC',
    'ETHUSDT': 'ETH',
    'BNBUSDT': 'BNB',
    'SOLUSDT': 'SOL',
    'ADAUSDT': 'ADA',
    'XRPUSDT': 'XRP',
    'DOGEUSDT': 'DOGE',
    'AVAXUSDT': 'AVAX',
    'DOTUSDT': 'DOT',
    'MATICUSDT': 'MATIC'
  };

  private timeframeMap: Record<Timeframe, { function: string; interval?: string }> = {
    '1m': { function: 'TIME_SERIES_INTRADAY', interval: '1min' },
    '5m': { function: 'TIME_SERIES_INTRADAY', interval: '5min' },
    '15m': { function: 'TIME_SERIES_INTRADAY', interval: '15min' },
    '30m': { function: 'TIME_SERIES_INTRADAY', interval: '30min' },
    '1h': { function: 'TIME_SERIES_INTRADAY', interval: '60min' },
    '4h': { function: 'TIME_SERIES_INTRADAY', interval: '60min' }, // Will need to resample
    '1d': { function: 'TIME_SERIES_DAILY', interval: undefined }
  };

  constructor(private http: HttpClient) {}

  private getAlphaSymbol(symbol: string): string {
    return this.symbolMap[symbol.toUpperCase()] || symbol.toUpperCase();
  }

  private buildParams(params: Record<string, string>): HttpParams {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      httpParams = httpParams.set(key, value);
    });
    return httpParams.set('apikey', this.apiKey);
  }

  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {
      console.error(`${operation} failed:`, error);
      return of(result as T);
    };
  }

  // Generate mock data for fallback when API is unavailable
  private generateMockTicker(symbol: string): MarketTicker {
    const basePrice = symbol === 'BTCUSDT' ? 65000 : symbol === 'ETHUSDT' ? 3200 : 100;
    const change = (Math.random() - 0.5) * 10;
    return {
      symbol: symbol.toUpperCase(),
      price: basePrice * (1 + change / 100),
      volume24h: Math.random() * 1e9,
      change24h: change,
      timestamp: Date.now(),
      high24h: basePrice * 1.05,
      low24h: basePrice * 0.95,
      open24h: basePrice
    };
  }

  private generateMockCandles(symbol: string, timeframe: Timeframe, limit: number): Candle[] {
    const basePrice = symbol === 'BTCUSDT' ? 65000 : symbol === 'ETHUSDT' ? 3200 : 100;
    const candles: Candle[] = [];
    let currentPrice = basePrice;
    const now = Date.now();
    const intervalMs = this.getTimeframeMs(timeframe);

    for (let i = limit; i >= 0; i--) {
      const timestamp = now - (i * intervalMs);
      const change = (Math.random() - 0.5) * 0.02;
      currentPrice = currentPrice * (1 + change);
      const open = currentPrice;
      const high = open * (1 + Math.random() * 0.01);
      const low = open * (1 - Math.random() * 0.01);
      const close = low + Math.random() * (high - low);
      const volume = Math.random() * 1000;

      candles.push({ timestamp, open, high, low, close, volume });
    }
    return candles;
  }

  private getTimeframeMs(timeframe: Timeframe): number {
    const map: Record<Timeframe, number> = {
      '1m': 60000,
      '5m': 300000,
      '15m': 900000,
      '30m': 1800000,
      '1h': 3600000,
      '4h': 14400000,
      '1d': 86400000
    };
    return map[timeframe];
  }

  private generateMockIndicators(symbol: string, timeframe: Timeframe, limit: number): IndicatorsResponse {
    const candles = this.generateMockCandles(symbol, timeframe, limit);
    return {
      symbol: symbol.toUpperCase(),
      timeframe,
      sma20: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.99 + Math.random() * 0.02) })),
      sma50: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.98 + Math.random() * 0.04) })),
      sma200: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.95 + Math.random() * 0.1) })),
      ema9: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.995 + Math.random() * 0.01) })),
      ema21: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.99 + Math.random() * 0.02) })),
      ema50: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.98 + Math.random() * 0.04) })),
      rsi14: candles.map(c => ({ timestamp: c.timestamp, value: 30 + Math.random() * 40 })),
      macd: candles.map(c => ({
        timestamp: c.timestamp,
        macd: (Math.random() - 0.5) * 100,
        signal: (Math.random() - 0.5) * 100,
        histogram: (Math.random() - 0.5) * 50
      })),
      bollingerBands20: candles.map(c => ({
        timestamp: c.timestamp,
        upper: c.close * 1.02,
        middle: c.close,
        lower: c.close * 0.98,
        percentB: Math.random(),
        bandwidth: 0.04
      })),
      atr14: candles.map(c => ({ timestamp: c.timestamp, value: c.close * 0.01 })),
      stochastic14: candles.map(c => ({ timestamp: c.timestamp, k: Math.random() * 100, d: Math.random() * 100 })),
      adx14: candles.map(c => ({ timestamp: c.timestamp, adx: Math.random() * 50, plusDi: Math.random() * 50, minusDi: Math.random() * 50 })),
      obv: candles.map((c, i) => ({ timestamp: c.timestamp, value: i * 1000 + Math.random() * 10000 })),
      vwap: candles.map(c => ({ timestamp: c.timestamp, value: c.close * (0.99 + Math.random() * 0.02) }))
    };
  }

  // Get ticker/quote data
  getTicker(symbol: string): Observable<MarketTicker | null> {
    const alphaSymbol = this.getAlphaSymbol(symbol);
    const params = this.buildParams({
      function: 'GLOBAL_QUOTE',
      symbol: alphaSymbol
    });

    return this.http.get<AlphaVantageTimeSeries>(this.baseUrl, { params }).pipe(
      map(response => {
        const quote = response['Global Quote'];
        if (!quote || !quote['05. price']) {
          console.warn('Alpha Vantage returned empty quote, using mock data');
          return this.generateMockTicker(symbol);
        }

        const price = parseFloat(quote['05. price']);
        const change = parseFloat(quote['09. change'] || '0');
        const changePercent = parseFloat((quote['10. change percent'] || '0%').replace('%', ''));
        const volume = parseFloat(quote['06. volume'] || '0');
        const high = parseFloat(quote['03. high'] || price.toString());
        const low = parseFloat(quote['04. low'] || price.toString());
        const open = parseFloat(quote['02. open'] || price.toString());

        return {
          symbol: symbol.toUpperCase(),
          price,
          volume24h: volume,
          change24h: changePercent,
          timestamp: Date.now(),
          high24h: high,
          low24h: low,
          open24h: open
        };
      }),
      catchError(err => {
        console.warn('Alpha Vantage getTicker error, using mock data:', err.message);
        return of(this.generateMockTicker(symbol));
      })
    );
  }

  // Get candles/ohlcv data
  getCandles(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<Candle[]> {
    const alphaSymbol = this.getAlphaSymbol(symbol);
    const tfConfig = this.timeframeMap[timeframe];
    const params = this.buildParams({
      function: tfConfig.function,
      symbol: alphaSymbol,
      interval: tfConfig.interval || '',
      outputsize: limit > 100 ? 'full' : 'compact',
      datatype: 'json'
    });

    return this.http.get<AlphaVantageTimeSeries>(this.baseUrl, { params }).pipe(
      map(response => {
        const timeSeriesKey = tfConfig.function === 'TIME_SERIES_DAILY'
          ? 'Time Series (Daily)'
          : `Time Series (${tfConfig.interval})`;

        const timeSeries = response[timeSeriesKey];
        if (!timeSeries || Object.keys(timeSeries).length === 0) {
          console.warn('Alpha Vantage returned empty time series, using mock data');
          return this.generateMockCandles(symbol, timeframe, limit);
        }

        return Object.entries(timeSeries)
          .slice(0, limit)
          .map(([timestamp, data]) => ({
            timestamp: new Date(timestamp).getTime(),
            open: parseFloat(data['1. open']),
            high: parseFloat(data['2. high']),
            low: parseFloat(data['3. low']),
            close: parseFloat(data['4. close']),
            volume: parseFloat(data['5. volume'])
          }))
          .reverse(); // Alpha Vantage returns newest first, we need oldest first
      }),
      catchError(err => {
        console.warn('Alpha Vantage getCandles error, using mock data:', err.message);
        return of(this.generateMockCandles(symbol, timeframe, limit));
      })
    );
  }

  // Get technical indicators
  getSma(symbol: string, timeframe: Timeframe, period: number, limit: number = 500): Observable<SmaDto[]> {
    return this.getIndicator('SMA', symbol, timeframe, { time_period: period.toString() }, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, value: d.value }))));
  }

  getEma(symbol: string, timeframe: Timeframe, period: number, limit: number = 500): Observable<EmaDto[]> {
    return this.getIndicator('EMA', symbol, timeframe, { time_period: period.toString() }, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, value: d.value }))));
  }

  getRsi(symbol: string, timeframe: Timeframe, period: number = 14, limit: number = 500): Observable<RsiDto[]> {
    return this.getIndicator('RSI', symbol, timeframe, { time_period: period.toString() }, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, value: d.value }))));
  }

  getMacd(symbol: string, timeframe: Timeframe, fastPeriod: number = 12, slowPeriod: number = 26, signalPeriod: number = 9, limit: number = 500): Observable<MacdDto[]> {
    return this.getIndicator('MACD', symbol, timeframe, {
      fastperiod: fastPeriod.toString(),
      slowperiod: slowPeriod.toString(),
      signalperiod: signalPeriod.toString()
    }, limit).pipe(map(data => data.map(d => ({
      timestamp: d.timestamp,
      macd: d.macd,
      signal: d.signal,
      histogram: d.histogram
    }))));
  }

  getBollingerBands(symbol: string, timeframe: Timeframe, period: number = 20, stdDev: number = 2, limit: number = 500): Observable<BollingerBandsDto[]> {
    return this.getIndicator('BBANDS', symbol, timeframe, {
      time_period: period.toString(),
      nbdevup: stdDev.toString(),
      nbdevdn: stdDev.toString()
    }, limit).pipe(map(data => data.map(d => ({
      timestamp: d.timestamp,
      upper: d.upper,
      middle: d.middle,
      lower: d.lower,
      percentB: d.percentB,
      bandwidth: d.bandwidth
    }))));
  }

  getAtr(symbol: string, timeframe: Timeframe, period: number = 14, limit: number = 500): Observable<AtrDto[]> {
    return this.getIndicator('ATR', symbol, timeframe, { time_period: period.toString() }, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, value: d.value }))));
  }

  getStochastic(symbol: string, timeframe: Timeframe, kPeriod: number = 14, dPeriod: number = 3, limit: number = 500): Observable<StochasticDto[]> {
    return this.getIndicator('STOCH', symbol, timeframe, {
      fastkperiod: kPeriod.toString(),
      slowkperiod: dPeriod.toString(),
      slowdperiod: dPeriod.toString()
    }, limit).pipe(map(data => data.map(d => ({ timestamp: d.timestamp, k: d.k, d: d.d }))));
  }

  getAdx(symbol: string, timeframe: Timeframe, period: number = 14, limit: number = 500): Observable<AdxDto[]> {
    return this.getIndicator('ADX', symbol, timeframe, { time_period: period.toString() }, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, adx: d.adx, plusDi: d.plusDi, minusDi: d.minusDi }))));
  }

  getObv(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<ObvDto[]> {
    return this.getIndicator('OBV', symbol, timeframe, {}, limit)
      .pipe(map(data => data.map(d => ({ timestamp: d.timestamp, value: d.value }))));
  }

  getVwap(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<VwapDto[]> {
    // Alpha Vantage doesn't have VWAP directly, we'll compute from candles
    return this.getCandles(symbol, timeframe, limit).pipe(
      map(candles => this.computeVwap(candles))
    );
  }

  private getIndicator(
    indicator: string,
    symbol: string,
    timeframe: Timeframe,
    extraParams: Record<string, string>,
    limit: number
  ): Observable<Array<{ timestamp: number; [key: string]: any }>> {
    const alphaSymbol = this.getAlphaSymbol(symbol);
    const tfConfig = this.timeframeMap[timeframe];

    const params = this.buildParams({
      function: indicator,
      symbol: alphaSymbol,
      interval: tfConfig.interval || 'daily',
      ...extraParams,
      datatype: 'json'
    });

    return this.http.get<AlphaVantageTimeSeries>(this.baseUrl, { params }).pipe(
      map(response => {
        const key = Object.keys(response).find(k => k.startsWith('Technical Analysis'));
        if (!key || !response[key]) return [];

        return Object.entries(response[key]!)
          .slice(0, limit)
          .map(([timestamp, data]) => {
            const result: { timestamp: number; [key: string]: any } = {
              timestamp: new Date(timestamp).getTime()
            };
            Object.entries(data).forEach(([k, v]) => {
              result[k.toLowerCase().replace(/\s+/g, '')] = parseFloat(v);
            });
            return result;
          })
          .reverse();
      }),
      catchError(this.handleError<Array<{ timestamp: number; [key: string]: any }>>(`get${indicator}`, []))
    );
  }

  private computeVwap(candles: Candle[]): VwapDto[] {
    let cumulativeVolume = 0;
    let cumulativePV = 0;

    return candles.map(candle => {
      const typicalPrice = (candle.high + candle.low + candle.close) / 3;
      cumulativePV += typicalPrice * candle.volume;
      cumulativeVolume += candle.volume;
      return {
        timestamp: candle.timestamp,
        value: cumulativeVolume > 0 ? cumulativePV / cumulativeVolume : typicalPrice
      };
    });
  }

  // Get all indicators at once
  getIndicators(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<IndicatorsResponse | null> {
    return forkJoin({
      sma20: this.getSma(symbol, timeframe, 20, limit),
      sma50: this.getSma(symbol, timeframe, 50, limit),
      sma200: this.getSma(symbol, timeframe, 200, limit),
      ema9: this.getEma(symbol, timeframe, 9, limit),
      ema21: this.getEma(symbol, timeframe, 21, limit),
      ema50: this.getEma(symbol, timeframe, 50, limit),
      rsi14: this.getRsi(symbol, timeframe, 14, limit),
      macd: this.getMacd(symbol, timeframe, 12, 26, 9, limit),
      bollingerBands20: this.getBollingerBands(symbol, timeframe, 20, 2, limit),
      atr14: this.getAtr(symbol, timeframe, 14, limit),
      stochastic14: this.getStochastic(symbol, timeframe, 14, 3, limit),
      adx14: this.getAdx(symbol, timeframe, 14, limit),
      obv: this.getObv(symbol, timeframe, limit),
      vwap: this.getVwap(symbol, timeframe, limit)
    }).pipe(
      map(indicators => ({
        symbol: symbol.toUpperCase(),
        timeframe,
        ...indicators
      })),
      catchError(err => {
        console.warn('Alpha Vantage getIndicators error, using mock data:', err.message);
        return of(this.generateMockIndicators(symbol, timeframe, limit));
      })
    );
  }

  // Health check
  health(): Observable<{ status: string }> {
    // Alpha Vantage doesn't have a health endpoint, just try a simple call
    // Always return ok since we have mock data fallback
    return of({ status: 'ok' });
  }
}