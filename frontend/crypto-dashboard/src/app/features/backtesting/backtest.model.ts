export interface Backtest {
  id: number;
  symbol: string;
  timeframe: string;
  startDate: string;
  endDate: string;
  minimumScore: number;
  totalTrades: number;
  wins: number;
  losses: number;
  winRate: number;
  profitFactor: number;
  netPnL: number;
  averageWin: number;
  averageLoss: number;
  maxDrawdown: number;
  averageR: number;
  createdAt: string;
}

export interface BacktestRequest {
  symbol: string;
  timeframe: string;
  startDate: string;
  endDate: string;
  minimumScore: number;
}