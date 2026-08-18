import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-scanner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="scanner-page">
      <h1>Market Scanner</h1>
      <p>Scan multiple symbols for trading opportunities</p>
    </div>
  `,
  styles: [`
    .scanner-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class ScannerComponent {}