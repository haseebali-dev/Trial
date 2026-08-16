import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-market',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="market-page">
      <h1>Market Detail</h1>
      <p>Market detail view for selected symbol</p>
    </div>
  `,
  styles: [`
    .market-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class MarketComponent {}