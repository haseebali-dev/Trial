import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription, interval, forkJoin, catchError, of, map } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { Signal, SignalSummaryDto } from './signal.model';
import { ChartComponent } from '../market/components/chart.component';
import { Candle, MarketTicker, Timeframe, EmaDto, BollingerBandsDto, VwapDto, IndicatorsResponse } from '../../core/models/market.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ChartComponent],
  template: `
    <div class="dashboard">
      <div class="top-bar">
        <h1>Crypto Trading Intelligence</h1>
        <div class="connection-status" [class.connected]="isConnected">
          <span class="status-dot"></span>
          <span>{{ isConnected ? 'Connected' : 'Disconnected' }}</span>
        </div>
      </div>

      <div class="dashboard-grid">
        <div class="main-chart-area">
          <app-chart
            #chartComponent
            [symbol]="symbol"
            [timeframe]="currentTimeframe"
            [candles]="candles"
            [ema9]="ema9"
            [ema21]="ema21"
            [ema50]="ema50"
            [bollingerBands]="bollingerBands"
            [vwap]="vwap"
            [showToolbar]="true"
            [showEma9]="true"
            [showEma21]="true"
            [showEma50]="true"
            [showBollingerBands]="true"
            [showVwap]="true"
            (timeframeChange)="onTimeframeChange($event)">
          </app-chart>
        </div>

        <div class="side-panel">
          <div class="panel market-bias">
            <h3>Market Bias</h3>
            <div class="bias-item" *ngFor="let bias of marketBias">
              <span class="timeframe">{{ bias.timeframe }}</span>
              <span class="direction" [class]="bias.direction.toLowerCase()">{{ bias.direction }}</span>
            </div>
          </div>

          <div class="panel signal">
            <h3>Signal</h3>
            <div class="signal-display" *ngIf="currentSignal" [class]="currentSignal.direction.toLowerCase()">
              <span class="signal-direction">{{ currentSignal.direction }}</span>
              <span class="signal-score">{{ currentSignal.score }}/{{ currentSignal.maxScore }}</span>
              <span class="signal-confidence">{{ currentSignal.confidenceLevel }}</span>
            </div>
            <div class="no-signal" *ngIf="!currentSignal">No signal data</div>
          </div>

          <div class="panel trade-setup" *ngIf="currentSignal && (currentSignal.direction === 'BUY' || currentSignal.direction === 'SELL')">
            <h3>Trade Setup</h3>
            <div class="setup-row">
              <span>Entry</span>
              <span>{{ currentSignal.entryPrice | number:'1.2-2' }}</span>
            </div>
            <div class="setup-row">
              <span>Stop Loss</span>
              <span>{{ currentSignal.stopLoss | number:'1.2-2' }}</span>
            </div>
            <div class="setup-row">
              <span>TP1</span>
              <span>{{ currentSignal.takeProfit1 | number:'1.2-2' }}</span>
            </div>
            <div class="setup-row">
              <span>TP2</span>
              <span>{{ currentSignal.takeProfit2 | number:'1.2-2' }}</span>
            </div>
            <div class="setup-row">
              <span>R:R</span>
              <span>{{ currentSignal.riskReward }}</span>
            </div>
          </div>

          <div class="panel ticker-info" *ngIf="ticker">
            <h3>{{ ticker.symbol }} Ticker</h3>
            <div class="ticker-row">
              <span>Price</span>
              <span class="price">{{ ticker.price | number:'1.2-2' }}</span>
            </div>
            <div class="ticker-row">
              <span>24h Change</span>
              <span class="change" [class.positive]="ticker.change24h >= 0" [class.negative]="ticker.change24h < 0">
                {{ ticker.change24h >= 0 ? '+' : '' }}{{ ticker.change24h | number:'1.2-2' }}%
              </span>
            </div>
            <div class="ticker-row">
              <span>24h Volume</span>
              <span>{{ formatVolume(ticker.volume24h) }}</span>
            </div>
            <div class="ticker-row">
              <span>24h High</span>
              <span>{{ ticker.high24h | number:'1.2-2' }}</span>
            </div>
            <div class="ticker-row">
              <span>24h Low</span>
              <span>{{ ticker.low24h | number:'1.2-2' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      height: 100vh;
      display: flex;
      flex-direction: column;
      background: #1e222d;
      color: #d1d4dc;
    }

    .top-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 20px;
      background: #131722;
      border-bottom: 1px solid #2a2e39;
    }

    .top-bar h1 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
    }

    .connection-status {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.875rem;
    }

    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #e53e3e;
    }

    .connection-status.connected .status-dot {
      background: #48bb78;
    }

    .dashboard-grid {
      flex: 1;
      display: grid;
      grid-template-columns: 1fr 320px;
      gap: 16px;
      padding: 16px;
      overflow: hidden;
    }

    .main-chart-area {
      background: #131722;
      border-radius: 8px;
      border: 1px solid #2a2e39;
      overflow: hidden;
      min-height: 0;
    }

    .side-panel {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .panel {
      background: #131722;
      border-radius: 8px;
      border: 1px solid #2a2e39;
      padding: 16px;
    }

    .panel h3 {
      margin: 0 0 16px;
      font-size: 0.875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #787b86;
    }

    .bias-item {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid #2a2e39;
    }

    .bias-item:last-child {
      border-bottom: none;
    }

    .timeframe {
      font-weight: 500;
    }

    .direction.bullish {
      color: #48bb78;
    }

    .direction.bearish {
      color: #e53e3e;
    }

    .direction.neutral {
      color: #f6ad55;
    }

    .signal-display {
      text-align: center;
      padding: 16px;
    }

    .signal-display.buy .signal-direction {
      color: #48bb78;
    }

    .signal-display.sell .signal-direction {
      color: #e53e3e;
    }

    .signal-display.wait .signal-direction {
      color: #f6ad55;
    }

    .signal-direction {
      display: block;
      font-size: 1.5rem;
      font-weight: 700;
      margin-bottom: 8px;
    }

    .signal-score {
      display: block;
      font-size: 1.25rem;
      font-weight: 600;
      color: #d1d4dc;
      margin-bottom: 4px;
    }

    .signal-confidence {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .signal-display.buy .signal-confidence {
      background: rgba(72, 187, 120, 0.2);
      color: #48bb78;
    }

    .signal-display.sell .signal-confidence {
      background: rgba(229, 62, 62, 0.2);
      color: #e53e3e;
    }

    .signal-display.wait .signal-confidence {
      background: rgba(246, 173, 85, 0.2);
      color: #f6ad55;
    }

    .no-signal {
      text-align: center;
      color: #787b86;
      padding: 16px;
    }

    .trade-setup {
      border-top: 1px solid #2a2e39;
      padding-top: 16px;
    }

    .setup-row {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
    }

    .setup-row span:first-child {
      color: #787b86;
    }

    .setup-row span:last-child {
      font-weight: 600;
    }

    .ticker-info {
      font-size: 0.875rem;
    }

    .ticker-row {
      display: flex;
      justify-content: space-between;
      padding: 6px 0;
      border-bottom: 1px solid #2a2e39;
    }

    .ticker-row:last-child {
      border-bottom: none;
    }

    .ticker-row span:first-child {
      color: #787b86;
    }

    .ticker-row .price {
      font-size: 1.25rem;
      font-weight: 600;
    }

    .ticker-row .change.positive {
      color: #48bb78;
    }

    .ticker-row .change.negative {
      color: #e53e3e;
    }

    @media (max-width: 1024px) {
      .dashboard-grid {
        grid-template-columns: 1fr;
      }

      .side-panel {
        flex-direction: row;
        flex-wrap: wrap;
      }

      .panel {
        flex: 1;
        min-width: 280px;
      }
    }
  `]
})
export class DashboardComponent implements OnInit, OnDestroy {
  isConnected = false;
  symbol = 'BTCUSDT';
  currentTimeframe: Timeframe = '1h';

  marketBias: Array<{ timeframe: string; direction: string }> = [
    { timeframe: '1H', direction: 'NEUTRAL' },
    { timeframe: '4H', direction: 'NEUTRAL' },
    { timeframe: '1D', direction: 'NEUTRAL' }
  ];
  currentSignal: Signal | null = null;

  // Chart data
  ticker: (MarketTicker & { high24h?: number; low24h?: number; open24h?: number }) | null = null;
  candles: Candle[] = [];
  ema9: EmaDto[] = [];
  ema21: EmaDto[] = [];
  ema50: EmaDto[] = [];
  bollingerBands: BollingerBandsDto[] = [];
  vwap: VwapDto[] = [];

  loading = false;
  error: string | null = null;

  private healthCheckSubscription?: Subscription;
  private refreshSubscription?: Subscription;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.checkHealth();
    this.healthCheckSubscription = interval(30000).subscribe(() => this.checkHealth());
    this.loadMarketData();

    // Auto-refresh every 30 seconds
    this.refreshSubscription = interval(30000).subscribe(() => {
      if (!this.loading) this.loadMarketData();
    });
  }

  ngOnDestroy(): void {
    this.healthCheckSubscription?.unsubscribe();
    this.refreshSubscription?.unsubscribe();
  }

  checkHealth(): void {
    this.apiService.health().subscribe({
      next: () => this.isConnected = true,
      error: () => this.isConnected = false
    });
  }

  loadMarketData(): void {
    this.loading = true;
    this.error = null;

    // Load ticker, candles, and indicators in parallel
    forkJoin({
      ticker: this.apiService.getTicker(this.symbol).pipe(
        catchError(err => {
          console.warn('Failed to load ticker:', err.message);
          return of(null);
        })
      ),
      candles: this.apiService.getCandles(this.symbol, this.currentTimeframe, 500).pipe(
        catchError(err => {
          console.warn('Failed to load candles:', err.message);
          return of([]);
        })
      ),
      indicators: this.apiService.getIndicators(this.symbol, this.currentTimeframe, 500).pipe(
        catchError(err => {
          console.warn('Failed to load indicators:', err.message);
          return of(null);
        })
      ),
      signalsSummary: this.apiService.getSignalsSummary(this.symbol, this.currentTimeframe).pipe(
        catchError(err => {
          console.warn('Failed to load signal summary:', err.message);
          return of(null);
        })
      )
    }).subscribe(({ ticker, candles, indicators, signalsSummary }) => {
      if (ticker) {
        this.ticker = ticker;
      }
      this.candles = candles;

      if (indicators) {
        this.ema9 = indicators.ema9 || [];
        this.ema21 = indicators.ema21 || [];
        this.ema50 = indicators.ema50 || [];
        this.bollingerBands = indicators.bollingerBands20 || [];
        this.vwap = indicators.vwap || [];
      }

      if (signalsSummary) {
        this.currentSignal = this.convertSignalSummary(signalsSummary);
      }

      this.loading = false;
    });
  }

  private convertSignalSummary(summary: SignalSummaryDto): Signal {
    // Handle empty signals array
    if (!summary.signals || summary.signals.length === 0) {
      return {
        id: Date.now(),
        symbol: summary.symbol,
        direction: 'WAIT',
        score: Math.round(summary.overallScore || 0),
        maxScore: 100,
        confidenceLevel: 'LOW',
        entryPrice: undefined,
        stopLoss: undefined,
        takeProfit1: undefined,
        takeProfit2: undefined,
        takeProfit3: undefined,
        riskReward: undefined,
        reasons: ['No signal data available'],
        timestamp: new Date(summary.timestamp).toISOString()
      };
    }

    // Find the best signal (highest confidence)
    const bestSignal = summary.signals.reduce((best, current) =>
      current.confidence > best.confidence ? current : best
    );

    // Map direction
    const direction = bestSignal.direction === 'LONG' ? 'BUY' :
                      bestSignal.direction === 'SHORT' ? 'SELL' : 'WAIT';

    // Map confidence level
    let confidenceLevel: 'HIGH' | 'MEDIUM' | 'LOW' = 'LOW';
    if (bestSignal.confidence >= 75) confidenceLevel = 'HIGH';
    else if (bestSignal.confidence >= 50) confidenceLevel = 'MEDIUM';

    // Calculate risk/reward if we have stop loss and take profit
    let riskReward: number | undefined;
    if (bestSignal.stopLoss && bestSignal.takeProfit) {
      const risk = Math.abs(bestSignal.entryPrice - bestSignal.stopLoss);
      const reward = Math.abs(bestSignal.takeProfit - bestSignal.entryPrice);
      if (risk > 0) {
        riskReward = parseFloat((reward / risk).toFixed(2));
      }
    }

    return {
      id: Date.now(),
      symbol: summary.symbol,
      direction,
      score: Math.round(summary.overallScore),
      maxScore: 100,
      confidenceLevel,
      entryPrice: bestSignal.entryPrice,
      stopLoss: bestSignal.stopLoss ?? undefined,
      takeProfit1: bestSignal.takeProfit ?? undefined,
      takeProfit2: undefined,
      takeProfit3: undefined,
      riskReward,
      reasons: [bestSignal.reason],
      timestamp: new Date(summary.timestamp).toISOString()
    };
  }

  onTimeframeChange(timeframe: Timeframe): void {
    this.currentTimeframe = timeframe;
    this.loadMarketData();
  }

  formatVolume(volume: number): string {
    if (volume >= 1e9) return (volume / 1e9).toFixed(2) + 'B';
    if (volume >= 1e6) return (volume / 1e6).toFixed(2) + 'M';
    if (volume >= 1e3) return (volume / 1e3).toFixed(2) + 'K';
    return volume.toFixed(2);
  }
}