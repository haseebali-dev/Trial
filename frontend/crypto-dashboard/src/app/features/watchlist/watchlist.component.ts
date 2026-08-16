import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="watchlist-page">
      <h1>Watchlist</h1>
      <p>Manage your trading watchlist</p>
    </div>
  `,
  styles: [`
    .watchlist-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class WatchlistComponent {}