import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-ai-analysis',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="ai-analysis-page">
      <h1>AI Analysis</h1>
      <p>Multiple AI model analysis and consensus</p>
    </div>
  `,
  styles: [`
    .ai-analysis-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class AIAnalysisComponent {}