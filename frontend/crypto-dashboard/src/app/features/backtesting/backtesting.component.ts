import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-backtesting',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="backtesting-page">
      <h1>Backtesting</h1>
      <p>Backtest your trading strategies</p>
    </div>
  `,
  styles: [`
    .backtesting-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class BacktestingComponent {}