import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Candle,
  MarketTicker,
  MarketOverview,
  Timeframe
} from '../models/market.models';
import { Signal } from '../../features/dashboard/signal.model';
import { AIAnalysis } from '../../features/ai-analysis/ai-analysis.model';
import { WatchlistItem } from '../../features/watchlist/watchlist.model';
import { TradeJournal } from '../../features/journal/journal.model';
import { Backtest, BacktestRequest } from '../../features/backtesting/backtest.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Health
  health(): Observable<{ status: string }> {
    return this.http.get<{ status: string }>(`${this.baseUrl}/market/health`);
  }

  // Market Data
  getTicker(symbol: string): Observable<MarketTicker> {
    return this.http.get<MarketTicker>(`${this.baseUrl}/market/ticker/${symbol}`);
  }

  getCandles(symbol: string, timeframe: Timeframe, limit: number = 500): Observable<Candle[]> {
    return this.http.get<Candle[]>(`${this.baseUrl}/market/candles/${symbol}/${timeframe}?limit=${limit}`);
  }

  getMarketOverview(): Observable<MarketOverview[]> {
    return this.http.get<MarketOverview[]>(`${this.baseUrl}/market/overview`);
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
}