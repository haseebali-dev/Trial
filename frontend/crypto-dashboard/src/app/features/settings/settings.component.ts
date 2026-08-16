import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="settings-page">
      <h1>Settings</h1>
      <p>Configure the application</p>
    </div>
  `,
  styles: [`
    .settings-page {
      padding: 20px;
      color: #d1d4dc;
    }
  `]
})
export class SettingsComponent {}