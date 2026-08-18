import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription, interval } from 'rxjs';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
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
          <div class="chart-placeholder">
            <h2>Chart Area</h2>
            <p>TradingView Lightweight Charts will be integrated here</p>
          </div>
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
    }

    .chart-placeholder {
      height: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      color: #787b86;
    }

    .chart-placeholder h2 {
      margin: 0 0 8px;
      color: #d1d4dc;
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
  marketBias: Array<{ timeframe: string; direction: string }> = [
    { timeframe: '1H', direction: 'NEUTRAL' },
    { timeframe: '4H', direction: 'NEUTRAL' },
    { timeframe: '1D', direction: 'NEUTRAL' }
  ];
  currentSignal: any = null;
  private healthCheckSubscription?: Subscription;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.checkHealth();
    this.healthCheckSubscription = interval(30000).subscribe(() => this.checkHealth());
    this.loadMarketData();
  }

  ngOnDestroy(): void {
    this.healthCheckSubscription?.unsubscribe();
  }

  checkHealth(): void {
    this.apiService.health().subscribe({
      next: () => this.isConnected = true,
      error: () => this.isConnected = false
    });
  }

  loadMarketData(): void {
    // Will be implemented when backend endpoints are ready
  }
}