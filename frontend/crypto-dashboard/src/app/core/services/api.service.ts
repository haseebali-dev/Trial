import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Candle,
  MarketTicker,
  MarketOverview,
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
  IndicatorsResponse,
  SignalDto,
  SignalSummaryDto,
  ScanResultDto,
  ScannerSummaryDto,
  ScanRequestDto,
  SymbolScanDetailsDto,
  ScannerStatusDto
} from '../models/market.models';
import { Signal } from '../../features/dashboard/signal.model';
import { AIAnalysis } from '../../features/ai-analysis/ai-analysis.model';
import { WatchlistItem } from '../../features/watchlist/watchlist.model';
import { TradeJournal } from '../../features/journal/journal.model';
import { Backtest, BacktestRequest } from '../../features/backtesting/backtest.model';
import { AlphaVantageService } from './alpha-vantage.service';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;
  private readonly useAlphaVantage = false; // Flag to use Alpha Vantage for market data

  constructor(
    private http: HttpClient,
    private alphaVantageService: AlphaVantageService
  ) {}

  // Health
  health(): Observable<{ status: string }> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.health();
    }
    return this.http.get<{ status: string }>(`${this.baseUrl}/market/health`);
  }

  // Market Data - Using Alpha Vantage
  getTicker(symbol: string): Observable<MarketTicker> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getTicker(symbol).pipe(
        map(ticker => ticker || { symbol, price: 0, volume24h: 0, change24h: 0, timestamp: Date.now() })
      );
    }
    return this.http.get<MarketTicker>(`${this.baseUrl}/market/ticker/${symbol}`);
  }

  getCandles(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<Candle[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getCandles(symbol, timeframe, limit);
    }
    return this.http.get<Candle[]>(`${this.baseUrl}/market/candles/${symbol}/${timeframe}?limit=${limit}`);
  }

  getMarketOverview(): Observable<MarketOverview[]> {
    if (this.useAlphaVantage) {
      // Return mock data for now - can be extended to fetch multiple symbols
      return of([]);
    }
    return this.http.get<MarketOverview[]>(`${this.baseUrl}/market/overview`);
  }

  // Technical Analysis - Using Alpha Vantage
  getIndicators(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<IndicatorsResponse> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getIndicators(symbol, timeframe, limit).pipe(
        map(indicators => indicators || {
          symbol: symbol.toUpperCase(),
          timeframe,
          sma20: [], sma50: [], sma200: [],
          ema9: [], ema21: [], ema50: [],
          rsi14: [],
          macd: [],
          bollingerBands20: [],
          atr14: [],
          stochastic14: [],
          adx14: [],
          obv: [],
          vwap: []
        })
      );
    }
    return this.http.get<IndicatorsResponse>(`${this.baseUrl}/indicators/${symbol}/${timeframe}?limit=${limit}`);
  }

  getSma(symbol: string, timeframe: Timeframe, period: number, limit: number = 500): Observable<SmaDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getSma(symbol, timeframe, period, limit);
    }
    return this.http.get<SmaDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/sma/${period}?limit=${limit}`);
  }

  getEma(symbol: string, timeframe: Timeframe, period: number, limit: number = 500): Observable<EmaDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getEma(symbol, timeframe, period, limit);
    }
    return this.http.get<EmaDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/ema/${period}?limit=${limit}`);
  }

  getRsi(symbol: string, timeframe: Timeframe, period: number = 14, limit: number = 500): Observable<RsiDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getRsi(symbol, timeframe, period, limit);
    }
    return this.http.get<RsiDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/rsi?period=${period}&limit=${limit}`);
  }

  getMacd(symbol: string, timeframe: Timeframe, fastPeriod: number = 12, slowPeriod: number = 26, signalPeriod: number = 9, limit: number = 500): Observable<MacdDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getMacd(symbol, timeframe, fastPeriod, slowPeriod, signalPeriod, limit);
    }
    return this.http.get<MacdDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/macd?fastPeriod=${fastPeriod}&slowPeriod=${slowPeriod}&signalPeriod=${signalPeriod}&limit=${limit}`);
  }

  getBollingerBands(symbol: string, timeframe: Timeframe, period: number = 20, stdDev: number = 2, limit: number = 500): Observable<BollingerBandsDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getBollingerBands(symbol, timeframe, period, stdDev, limit);
    }
    return this.http.get<BollingerBandsDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/bollinger?period=${period}&stdDev=${stdDev}&limit=${limit}`);
  }

  getAtr(symbol: string, timeframe: Timeframe, period: number = 14, limit: number = 500): Observable<AtrDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getAtr(symbol, timeframe, period, limit);
    }
    return this.http.get<AtrDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/atr?period=${period}&limit=${limit}`);
  }

  getStochastic(symbol: string, timeframe: Timeframe, kPeriod: number = 14, dPeriod: number = 3, limit: number = 500): Observable<StochasticDto[]> {
    if (this.useAlphaVantage) {
      return this.alphaVantageService.getStochastic(symbol, timeframe, kPeriod, dPeriod, limit);
    }
    return this.http.get<StochasticDto[]>(`${this.baseUrl}/indicators/${symbol}/${timeframe}/stochastic?kPeriod=${kPeriod}&dPeriod=${dPeriod}&limit=${limit}`);
  }

  getSignalsSummary(symbol: string, timeframe: Timeframe, strategies?: string[]): Observable<SignalSummaryDto> {
    const params = new URLSearchParams();
    if (strategies?.length) {
      params.append('strategies', strategies.join(','));
    }
    return this.http.get<SignalSummaryDto>(`${this.baseUrl}/signals/${symbol}/${timeframe}?${params.toString()}`);
  }

  getMarketBias(symbol: string, timeframe: Timeframe): Observable<{ direction: string; strength: number; reasons: string[] }> {
    return this.http.get<{ direction: string; strength: number; reasons: string[] }>(`${this.baseUrl}/smc/${symbol}/${timeframe}/bias`);
  }

  // Analysis
  getAnalysis(symbol: string, timeframe?: Timeframe): Observable<any> {
    const url = timeframe
      ? `${this.baseUrl}/analysis/${symbol}/${timeframe}`
      : `${this.baseUrl}/analysis/${symbol}`;
    return this.http.get(url);
  }

  // Signals
  getSignals(): Observable<Signal[]> {
    return this.http.get<Signal[]>(`${this.baseUrl}/signals`);
  }

  getSignal(symbol: string): Observable<Signal> {
    return this.http.get<Signal>(`${this.baseUrl}/signals/${symbol}`);
  }

  // AI
  getAIModels(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/ai/models`);
  }

  analyzeWithAI(request: { symbol: string; timeframe: Timeframe; model?: string }): Observable<AIAnalysis> {
    return this.http.post<AIAnalysis>(`${this.baseUrl}/ai/analyze`, request);
  }

  getAIConsensus(symbol: string, timeframe: Timeframe, models: string[]): Observable<any> {
    return this.http.post(`${this.baseUrl}/ai/consensus`, { symbol, timeframe, models });
  }

  // Watchlist
  getWatchlist(): Observable<WatchlistItem[]> {
    return this.http.get<WatchlistItem[]>(`${this.baseUrl}/watchlist`);
  }

  addToWatchlist(item: { symbol: string; name?: string; isFavorite?: boolean; defaultTimeframe?: string }): Observable<WatchlistItem> {
    return this.http.post<WatchlistItem>(`${this.baseUrl}/watchlist`, item);
  }

  removeFromWatchlist(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/watchlist/${id}`);
  }

  updateWatchlistItem(id: number, item: Partial<WatchlistItem>): Observable<WatchlistItem> {
    return this.http.put<WatchlistItem>(`${this.baseUrl}/watchlist/${id}`, item);
  }

  // Journal
  getJournal(): Observable<TradeJournal[]> {
    return this.http.get<TradeJournal[]>(`${this.baseUrl}/journal`);
  }

  addJournalEntry(entry: Omit<TradeJournal, 'id' | 'createdAt' | 'updatedAt'>): Observable<TradeJournal> {
    return this.http.post<TradeJournal>(`${this.baseUrl}/journal`, entry);
  }

  updateJournalEntry(id: number, entry: Partial<TradeJournal>): Observable<TradeJournal> {
    return this.http.put<TradeJournal>(`${this.baseUrl}/journal/${id}`, entry);
  }

  deleteJournalEntry(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/journal/${id}`);
  }

  // Backtesting
  runBacktest(request: BacktestRequest): Observable<Backtest> {
    return this.http.post<Backtest>(`${this.baseUrl}/backtest`, request);
  }

  getBacktests(): Observable<Backtest[]> {
    return this.http.get<Backtest[]>(`${this.baseUrl}/backtest`);
  }

  // Settings
  getSettings(): Observable<any> {
    return this.http.get(`${this.baseUrl}/settings`);
  }

  updateSettings(settings: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/settings`, settings);
  }

  // Scanner
  scanMarket(request: ScanRequestDto): Observable<ScannerSummaryDto> {
    return this.http.post<ScannerSummaryDto>(`${this.baseUrl}/scanner/scan`, request);
  }

  scanMarketGet(symbols?: string[], timeframes?: string[], minimumScore?: number, maxConcurrentScans?: number): Observable<ScannerSummaryDto> {
    const params = new URLSearchParams();
    if (symbols?.length) params.append('symbols', symbols.join(','));
    if (timeframes?.length) params.append('timeframes', timeframes.join(','));
    if (minimumScore) params.append('minimumScore', minimumScore.toString());
    if (maxConcurrentScans) params.append('maxConcurrentScans', maxConcurrentScans.toString());
    return this.http.get<ScannerSummaryDto>(`${this.baseUrl}/scanner/scan?${params.toString()}`);
  }

  scanSymbol(symbol: string, timeframe: string): Observable<SymbolScanDetailsDto> {
    return this.http.get<SymbolScanDetailsDto>(`${this.baseUrl}/scanner/symbol/${symbol}/${timeframe}`);
  }

  scanSymbols(symbols: string[], timeframe: string, minimumScore: number = 5): Observable<ScanResultDto[]> {
    return this.http.post<ScanResultDto[]>(`${this.baseUrl}/scanner/symbols`, { symbols, timeframe, minimumScore });
  }

  getScannerStatus(): Observable<ScannerStatusDto> {
    return this.http.get<ScannerStatusDto>(`${this.baseUrl}/scanner/status`);
  }
}