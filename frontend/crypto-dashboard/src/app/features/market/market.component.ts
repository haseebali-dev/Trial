import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subject, takeUntil, switchMap, catchError, of, interval } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ChartComponent } from './components/chart.component';
import { Candle, MarketTicker, Timeframe, TIMEFRAMES, TIMEFRAME_LABELS } from '../../core/models/market.models';

@Component({
  selector: 'app-market',
  standalone: true,
  imports: [CommonModule, RouterModule, ChartComponent],
  template: `
    <div class="market-page">
      <!-- Top Bar -->
      <div class="top-bar">
        <div class="symbol-info" *ngIf="ticker">
          <h1>{{ ticker.symbol }}</h1>
          <div class="price-row">
            <span class="current-price">{{ ticker.price | number:'1.2-2' }}</span>
            <span class="change-24h" [class.positive]="ticker.change24h >= 0" [class.negative]="ticker.change24h < 0">
              {{ ticker.change24h >= 0 ? '+' : '' }}{{ ticker.change24h | number:'1.2-2' }}%
            </span>
          </div>
          <div class="volume-row">
            <span>Vol 24h: {{ formatVolume(ticker.volume24h) }}</span>
          </div>
        </div>
        <div class="connection-status" [class.connected]="isConnected">
          <span class="status-dot"></span>
          <span>{{ isConnected ? 'Live' : 'Offline' }}</span>
        </div>
      </div>

      <!-- Chart Area -->
      <div class="chart-area">
        <app-chart
          #chartComponent
          [symbol]="symbol"
          [timeframe]="currentTimeframe"
          [candles]="candles"
          [showToolbar]="true"
          (timeframeChange)="onTimeframeChange($event)">
        </app-chart>
      </div>

      <!-- Stats Bar -->
      <div class="stats-bar" *ngIf="ticker">
        <div class="stat">
          <span class="stat-label">High 24h</span>
          <span class="stat-value">{{ ticker.high24h | number:'1.2-2' }}</span>
        </div>
        <div class="stat">
          <span class="stat-label">Low 24h</span>
          <span class="stat-value">{{ ticker.low24h | number:'1.2-2' }}</span>
        </div>
        <div class="stat">
          <span class="stat-label">Open 24h</span>
          <span class="stat-value">{{ ticker.open24h | number:'1.2-2' }}</span>
        </div>
        <div class="stat">
          <span class="stat-label">Volume</span>
          <span class="stat-value">{{ formatVolume(ticker.volume24h) }}</span>
        </div>
      </div>

      <!-- Loading / Error -->
      <div class="loading-overlay" *ngIf="loading && !candles.length">
        <div class="spinner"></div>
        <p>Loading chart data...</p>
      </div>

      <div class="error-message" *ngIf="error && !candles.length">
        <p>{{ error }}</p>
        <button class="retry-btn" (click)="loadData()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .market-page {
      height: 100vh;
      display: flex;
      flex-direction: column;
      background: #1e222d;
      color: #d1d4dc;
    }

    .top-bar {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 16px 20px;
      background: #131722;
      border-bottom: 1px solid #2a2e39;
    }

    .symbol-info h1 {
      margin: 0 0 8px;
      font-size: 1.5rem;
      font-weight: 600;
    }

    .price-row {
      display: flex;
      align-items: baseline;
      gap: 12px;
    }

    .current-price {
      font-size: 1.75rem;
      font-weight: 600;
      font-variant-numeric: tabular-nums;
    }

    .change-24h {
      font-size: 1rem;
      font-weight: 500;
      padding: 2px 8px;
      border-radius: 4px;
    }

    .change-24h.positive {
      color: #48bb78;
      background: rgba(72, 187, 120, 0.1);
    }

    .change-24h.negative {
      color: #e53e3e;
      background: rgba(229, 62, 62, 0.1);
    }

    .volume-row {
      margin-top: 4px;
      font-size: 0.875rem;
      color: #787b86;
    }

    .connection-status {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 12px;
      background: #1e222d;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
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

    .chart-area {
      flex: 1;
      position: relative;
      min-height: 500px;
    }

    .stats-bar {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      padding: 16px 20px;
      background: #131722;
      border-top: 1px solid #2a2e39;
    }

    .stat {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .stat-label {
      font-size: 0.75rem;
      color: #787b86;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .stat-value {
      font-size: 1rem;
      font-weight: 500;
      font-variant-numeric: tabular-nums;
    }

    .loading-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: rgba(19, 23, 34, 0.9);
      z-index: 10;
      color: #787b86;
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid #2a2e39;
      border-top-color: #2962ff;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .error-message {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: #131722;
      z-index: 10;
      color: #e53e3e;
      padding: 20px;
      text-align: center;
    }

    .retry-btn {
      margin-top: 16px;
      padding: 8px 20px;
      background: #2962ff;
      border: none;
      border-radius: 4px;
      color: #fff;
      font-weight: 500;
      cursor: pointer;
    }

    .retry-btn:hover {
      background: #1a56e8;
    }

    @media (max-width: 768px) {
      .stats-bar {
        grid-template-columns: repeat(2, 1fr);
      }

      .top-bar {
        flex-direction: column;
        gap: 16px;
        align-items: flex-start;
      }
    }
  `]
})
export class MarketComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild(ChartComponent) chartComponent!: ChartComponent;

  symbol = 'BTCUSDT';
  currentTimeframe: Timeframe = '1h';
  timeframes = TIMEFRAMES;
  timeframeLabels = TIMEFRAME_LABELS;

  ticker: (MarketTicker & { high24h?: number; low24h?: number; open24h?: number }) | null = null;
  candles: Candle[] = [];
  loading = false;
  error: string | null = null;
  isConnected = false;

  private destroy$ = new Subject<void>();
  private refreshSubscription?: any;

  constructor(
    private route: ActivatedRoute,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    // Get symbol from route params
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const symbol = params.get('symbol');
      if (symbol) {
        this.symbol = symbol.toUpperCase();
        this.loadData();
      }
    });

    // Get timeframe from query params
    this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const tf = params.get('timeframe') as Timeframe;
      if (tf && this.timeframes.includes(tf)) {
        this.currentTimeframe = tf;
      }
    });

    // Health check
    this.checkHealth();
    interval(30000).pipe(takeUntil(this.destroy$)).subscribe(() => this.checkHealth());

    // Auto-refresh ticker every 10 seconds
    this.refreshSubscription = interval(10000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      if (!this.loading) this.loadTicker();
    });
  }

  ngAfterViewInit(): void {
    // Chart is initialized via ViewChild
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshSubscription?.unsubscribe();
  }

  checkHealth(): void {
    this.apiService.health().subscribe({
      next: () => this.isConnected = true,
      error: () => this.isConnected = false
    });
  }

  loadData(): void {
    this.loading = true;
    this.error = null;

    // Load ticker and candles in parallel
    this.apiService.getTicker(this.symbol).pipe(
      takeUntil(this.destroy$),
      catchError(err => {
        this.error = 'Failed to load ticker: ' + err.message;
        return of(null);
      })
    ).subscribe(ticker => {
      if (ticker) {
        this.ticker = ticker;
      }
    });

    this.apiService.getCandles(this.symbol, this.currentTimeframe, 500).pipe(
      takeUntil(this.destroy$),
      catchError(err => {
        this.error = 'Failed to load candles: ' + err.message;
        this.loading = false;
        return of([]);
      })
    ).subscribe(candles => {
      this.candles = candles;
      this.loading = false;
      // Update chart component
      if (this.chartComponent) {
        this.chartComponent.updateData(candles);
      }
    });
  }

  loadTicker(): void {
    this.apiService.getTicker(this.symbol).pipe(
      takeUntil(this.destroy$),
      catchError(err => of(null))
    ).subscribe(ticker => {
      if (ticker) {
        this.ticker = ticker;
      }
    });
  }

  onTimeframeChange(timeframe: Timeframe): void {
    this.currentTimeframe = timeframe;
    this.loadData();
  }

  formatVolume(volume: number): string {
    if (volume >= 1e9) return (volume / 1e9).toFixed(2) + 'B';
    if (volume >= 1e6) return (volume / 1e6).toFixed(2) + 'M';
    if (volume >= 1e3) return (volume / 1e3).toFixed(2) + 'K';
    return volume.toFixed(2);
  }
}