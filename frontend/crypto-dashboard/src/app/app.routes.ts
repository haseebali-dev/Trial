import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'market/:symbol',
    loadComponent: () => import('./features/market/market.component').then(m => m.MarketComponent)
  },
  {
    path: 'scanner',
    loadComponent: () => import('./features/scanner/scanner.component').then(m => m.ScannerComponent)
  },
  {
    path: 'ai-analysis',
    loadComponent: () => import('./features/ai-analysis/ai-analysis.component').then(m => m.AIAnalysisComponent)
  },
  {
    path: 'watchlist',
    loadComponent: () => import('./features/watchlist/watchlist.component').then(m => m.WatchlistComponent)
  },
  {
    path: 'journal',
    loadComponent: () => import('./features/journal/journal.component').then(m => m.JournalComponent)
  },
  {
    path: 'backtesting',
    loadComponent: () => import('./features/backtesting/backtesting.component').then(m => m.BacktestingComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent)
  }
];