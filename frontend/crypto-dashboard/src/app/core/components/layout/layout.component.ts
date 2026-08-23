import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="app-layout">
      <!-- Sidebar -->
      <aside class="sidebar" [class.collapsed]="sidebarCollapsed">
        <div class="sidebar-header">
          <div class="logo">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
            </svg>
            <span class="logo-text" *ngIf="!sidebarCollapsed">CryptoTrader</span>
          </div>
          <button class="collapse-btn" (click)="toggleSidebar()" aria-label="Toggle sidebar">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M15 18l-6-6 6-6" *ngIf="!sidebarCollapsed"/>
              <path d="M9 18l6-6-6-6" *ngIf="sidebarCollapsed"/>
            </svg>
          </button>
        </div>

        <nav class="sidebar-nav">
          <ul class="nav-list">
            <li class="nav-item">
              <a class="nav-link" routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Dashboard</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/market/BTCUSDT" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M18 20V10M12 20V4M6 20v-6"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Market</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/scanner" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Scanner</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/ai-analysis" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 2a4 4 0 0 1 4 4c0 1.31-.52 2.5-1.34 3.35M12 22a4 4 0 0 0 4-4c0-1.31.52-2.5 1.34-3.35M2 12a4 4 0 0 1 4-4c1.31 0 2.5.52 3.35 1.34M22 12a4 4 0 0 0-4 4c1.31 0 2.5-.52 3.35-1.34"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">AI Analysis</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/watchlist" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Watchlist</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/journal" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Journal</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/backtesting" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 3v18h18"/><path d="M7 16l4-4 4 4 4-4 4 4"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Backtesting</span>
              </a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/settings" routerLinkActive="active">
                <svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
                </svg>
                <span class="nav-label" *ngIf="!sidebarCollapsed">Settings</span>
              </a>
            </li>
          </ul>
        </nav>

        <div class="sidebar-footer" *ngIf="!sidebarCollapsed">
          <div class="connection-status" [class.connected]="isConnected">
            <span class="status-dot"></span>
            <span>{{ isConnected ? 'Connected' : 'Disconnected' }}</span>
          </div>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="main-content" [class.sidebar-collapsed]="sidebarCollapsed">
        <!-- Top Bar -->
        <header class="top-bar">
          <div class="top-bar-left">
            <button class="menu-toggle" (click)="toggleSidebar()" aria-label="Toggle menu">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="3" y1="12" x2="21" y2="12"/>
                <line x1="3" y1="6" x2="21" y2="6"/>
                <line x1="3" y1="18" x2="21" y2="18"/>
              </svg>
            </button>
            <h1 class="page-title" *ngIf="sidebarCollapsed">{{ getPageTitle() }}</h1>
          </div>
          <div class="top-bar-right">
            <div class="connection-indicator" [class.connected]="isConnected">
              <span class="status-dot"></span>
              <span class="status-text" *ngIf="!sidebarCollapsed">{{ isConnected ? 'Live' : 'Offline' }}</span>
            </div>
            <div class="user-menu">
              <button class="user-btn">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
                </svg>
              </button>
            </div>
          </div>
        </header>

        <!-- Page Content -->
        <div class="page-content">
          <ng-content></ng-content>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .app-layout {
      display: flex;
      height: 100vh;
      background: #131722;
      color: #d1d4dc;
      font-family: "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    }

    /* Sidebar */
    .sidebar {
      width: 260px;
      background: #1e222d;
      border-right: 1px solid #2a2e39;
      display: flex;
      flex-direction: column;
      transition: width 0.3s ease;
      z-index: 100;
    }

    .sidebar.collapsed {
      width: 72px;
    }

    .sidebar-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      border-bottom: 1px solid #2a2e39;
      min-height: 64px;
    }

    .logo {
      display: flex;
      align-items: center;
      gap: 12px;
      color: #2962ff;
    }

    .logo svg {
      flex-shrink: 0;
    }

    .logo-text {
      font-size: 1.25rem;
      font-weight: 700;
      white-space: nowrap;
      overflow: hidden;
    }

    .collapse-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: transparent;
      border: 1px solid #2a2e39;
      border-radius: 6px;
      color: #787b86;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .collapse-btn:hover {
      background: #2a2e39;
      color: #d1d4dc;
    }

    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: 12px 8px;
    }

    .nav-list {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .nav-item {
      margin-bottom: 4px;
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 8px;
      color: #787b86;
      text-decoration: none;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .nav-link:hover {
      background: #2a2e39;
      color: #d1d4dc;
    }

    .nav-link.active {
      background: rgba(41, 98, 255, 0.15);
      color: #2962ff;
    }

    .nav-link.active .nav-icon {
      color: #2962ff;
    }

    .nav-icon {
      flex-shrink: 0;
      transition: color 0.2s ease;
    }

    .nav-label {
      font-size: 0.875rem;
      font-weight: 500;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sidebar.collapsed .nav-link {
      justify-content: center;
      padding: 12px;
    }

    .sidebar.collapsed .nav-label {
      display: none;
    }

    .sidebar-footer {
      padding: 16px;
      border-top: 1px solid #2a2e39;
    }

    .connection-status {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 12px;
      background: #131722;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 500;
      color: #787b86;
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

    /* Main Content */
    .main-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0;
      transition: margin-left 0.3s ease;
    }

    /* Top Bar */
    .top-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 64px;
      padding: 0 24px;
      background: #1e222d;
      border-bottom: 1px solid #2a2e39;
    }

    .top-bar-left {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .menu-toggle {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: transparent;
      border: 1px solid #2a2e39;
      border-radius: 8px;
      color: #787b86;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .menu-toggle:hover {
      background: #2a2e39;
      color: #d1d4dc;
    }

    .page-title {
      display: none;
      font-size: 1.25rem;
      font-weight: 600;
      margin: 0;
    }

    .main-content.sidebar-collapsed .page-title {
      display: block;
    }

    .top-bar-right {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .connection-indicator {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 12px;
      background: #131722;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .connection-indicator .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #e53e3e;
    }

    .connection-indicator.connected .status-dot {
      background: #48bb78;
    }

    .user-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: #2a2e39;
      border: none;
      border-radius: 8px;
      color: #787b86;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .user-btn:hover {
      background: #3a3e49;
      color: #d1d4dc;
    }

    /* Page Content */
    .page-content {
      flex: 1;
      overflow-y: auto;
      padding: 24px;
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .sidebar {
        position: fixed;
        left: 0;
        top: 0;
        bottom: 0;
        z-index: 200;
        transform: translateX(-100%);
      }

      .sidebar.open {
        transform: translateX(0);
      }

      .main-content {
        margin-left: 0;
      }

      .menu-toggle {
        display: flex;
      }
    }

    @media (max-width: 768px) {
      .page-content {
        padding: 16px;
      }

      .top-bar {
        padding: 0 16px;
      }
    }

    /* Scrollbar */
    .sidebar-nav::-webkit-scrollbar {
      width: 6px;
    }

    .sidebar-nav::-webkit-scrollbar-track {
      background: transparent;
    }

    .sidebar-nav::-webkit-scrollbar-thumb {
      background: #2a2e39;
      border-radius: 3px;
    }

    .sidebar-nav::-webkit-scrollbar-thumb:hover {
      background: #3a3e49;
    }
  `]
})
export class LayoutComponent {
  sidebarCollapsed = false;
  isConnected = true;

  constructor(private router: Router) {}

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  getPageTitle(): string {
    const url = this.router.url;
    if (url.startsWith('/dashboard')) return 'Dashboard';
    if (url.startsWith('/market')) return 'Market';
    if (url.startsWith('/scanner')) return 'Scanner';
    if (url.startsWith('/ai-analysis')) return 'AI Analysis';
    if (url.startsWith('/watchlist')) return 'Watchlist';
    if (url.startsWith('/journal')) return 'Journal';
    if (url.startsWith('/backtesting')) return 'Backtesting';
    if (url.startsWith('/settings')) return 'Settings';
    return 'CryptoTrader';
  }
}