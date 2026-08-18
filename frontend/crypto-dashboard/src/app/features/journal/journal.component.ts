import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="journal-page">
      <h1>Trade Journal</h1>
      <p>Record and review your trades</p>
    </div>
  `,
  styles: [`
    .journal-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class JournalComponent {}