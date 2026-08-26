import { Injectable, NgZone } from '@angular/core';
import { Observable, Subject, BehaviorSubject } from 'rxjs';
import { Candle } from '../models/market.models';

interface BinanceKlineData {
  e: string; // Event type
  E: number; // Event time
  s: string; // Symbol
  k: {
    t: number; // Kline start time
    T: number; // Kline close time
    s: string; // Symbol
    i: string; // Interval
    f: number; // First trade ID
    L: number; // Last trade ID
    o: string; // Open price
    c: string; // Close price
    h: string; // High price
    l: string; // Low price
    v: string; // Base asset volume
    n: number; // Number of trades
    x: boolean; // Is this kline closed?
    q: string; // Quote asset volume
    V: string; // Taker buy base asset volume
    Q: string; // Taker buy quote asset volume
    B: string; // Ignore
  };
}

interface BinanceTickerData {
  e: string; // Event type
  E: number; // Event time
  s: string; // Symbol
  p: string; // Price change
  P: string; // Price change percent
  w: string; // Weighted average price
  x: string; // Previous day's close price
  c: string; // Current day's close price
  Q: string; // Close trade's quantity
  b: string; // Best bid price
  B: string; // Best bid quantity
  a: string; // Best ask price
  A: string; // Best ask quantity
  o: string; // Open price
  h: string; // High price
  l: string; // Low price
  v: string; // Total traded base asset volume
  q: string; // Total traded quote asset volume
  O: number; // Statistics open time
  C: number; // Statistics close time
  F: number; // First trade ID
  L: number; // Last trade ID
  n: number; // Total number of trades
}

type Timeframe = '1m' | '5m' | '15m' | '30m' | '1h' | '4h' | '1d';

const BINANCE_TIMEFRAME_MAP: Record<Timeframe, string> = {
  '1m': '1m',
  '5m': '5m',
  '15m': '15m',
  '30m': '30m',
  '1h': '1h',
  '4h': '4h',
  '1d': '1d'
};

@Injectable({
  providedIn: 'root'
})
export class BinanceWebsocketService {
  private ws: WebSocket | null = null;
  private currentSymbol = 'BTCUSDT';
  private currentTimeframe: Timeframe = '1m';
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 3000;
  private isConnecting = false;
  private shouldReconnect = true;

  // Subjects for real-time data
  private candleSubject = new BehaviorSubject<Candle | null>(null);
  private candleClosedSubject = new Subject<Candle>();
  private tickerSubject = new BehaviorSubject<{ price: number; change24h: number; volume24h: number; high24h: number; low24h: number; open24h: number } | null>(null);
  private connectionStatusSubject = new BehaviorSubject<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');

  // Public observables
  public candle$ = this.candleSubject.asObservable();
  public candleClosed$ = this.candleClosedSubject.asObservable();
  public ticker$ = this.tickerSubject.asObservable();
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  constructor(private ngZone: NgZone) {}

  connect(symbol: string = 'BTCUSDT', timeframe: Timeframe = '1m'): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.disconnect();
    }

    this.currentSymbol = symbol.toUpperCase();
    this.currentTimeframe = timeframe;
    this.shouldReconnect = true;
    this.establishConnection();
  }

  private establishConnection(): void {
    if (this.isConnecting) return;

    this.isConnecting = true;
    this.ngZone.run(() => this.connectionStatusSubject.next('connecting'));

    // Binance combined stream: kline + 24hr ticker
    const klineStream = `${this.currentSymbol.toLowerCase()}@kline_${BINANCE_TIMEFRAME_MAP[this.currentTimeframe]}`;
    const tickerStream = `${this.currentSymbol.toLowerCase()}@ticker`;
    const wsUrl = `wss://stream.binance.com:9443/stream?streams=${klineStream}/${tickerStream}`;

    try {
      this.ws = new WebSocket(wsUrl);
      this.ws.binaryType = 'arraybuffer';

      this.ws.onopen = () => {
        console.log('[Binance WS] Connected to', wsUrl);
        this.ngZone.run(() => {
          this.connectionStatusSubject.next('connected');
          this.reconnectAttempts = 0;
          this.isConnecting = false;
        });
      };

      this.ws.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          if (data.stream && data.data) {
            if (data.stream.includes('@kline_')) {
              this.handleKlineMessage(data.data);
            } else if (data.stream.includes('@ticker')) {
              this.handleTickerMessage(data.data);
            }
          }
        } catch (e) {
          console.warn('[Binance WS] Failed to parse message:', e);
        }
      };

      this.ws.onerror = (error) => {
        console.error('[Binance WS] Error:', error);
        this.ngZone.run(() => this.connectionStatusSubject.next('error'));
      };

      this.ws.onclose = (event) => {
        console.log('[Binance WS] Disconnected:', event.code, event.reason);
        this.ngZone.run(() => this.connectionStatusSubject.next('disconnected'));
        this.isConnecting = false;

        if (this.shouldReconnect && this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++;
          console.log(`[Binance WS] Reconnecting... (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
          setTimeout(() => this.establishConnection(), this.reconnectDelay * this.reconnectAttempts);
        }
      };
    } catch (e) {
      console.error('[Binance WS] Failed to create connection:', e);
      this.isConnecting = false;
      this.ngZone.run(() => this.connectionStatusSubject.next('error'));
    }
  }

  private handleKlineMessage(data: BinanceKlineData): void {
    const k = data.k;
    const candle: Candle = {
      timestamp: k.t,
      open: parseFloat(k.o),
      high: parseFloat(k.h),
      low: parseFloat(k.l),
      close: parseFloat(k.c),
      volume: parseFloat(k.v)
    };

    this.ngZone.run(() => {
      // Emit real-time updates (open candle)
      this.candleSubject.next(candle);

      // If candle is closed, emit to closed subject
      if (k.x) {
        this.candleClosedSubject.next(candle);
      }
    });
  }

  private handleTickerMessage(data: BinanceTickerData): void {
    const ticker = {
      price: parseFloat(data.c),
      change24h: parseFloat(data.P),
      volume24h: parseFloat(data.v),
      high24h: parseFloat(data.h),
      low24h: parseFloat(data.l),
      open24h: parseFloat(data.o)
    };

    this.ngZone.run(() => {
      this.tickerSubject.next(ticker);
    });
  }

  disconnect(): void {
    this.shouldReconnect = false;
    this.reconnectAttempts = this.maxReconnectAttempts; // Prevent reconnection

    if (this.ws) {
      this.ws.close(1000, 'Client disconnected');
      this.ws = null;
    }
    this.ngZone.run(() => this.connectionStatusSubject.next('disconnected'));
  }

  switchSymbol(symbol: string, timeframe?: Timeframe): void {
    this.connect(symbol, timeframe || this.currentTimeframe);
  }

  switchTimeframe(timeframe: Timeframe): void {
    this.connect(this.currentSymbol, timeframe);
  }

  isConnected(): boolean {
    return this.ws?.readyState === WebSocket.OPEN;
  }

  getConnectionStatus(): Observable<'connecting' | 'connected' | 'disconnected' | 'error'> {
    return this.connectionStatus$;
  }

  // Get latest candle snapshot
  getLatestCandle(): Candle | null {
    return this.candleSubject.value;
  }

  // Get latest ticker snapshot
  getLatestTicker(): { price: number; change24h: number; volume24h: number; high24h: number; low24h: number; open24h: number } | null {
    return this.tickerSubject.value;
  }
}