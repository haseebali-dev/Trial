export interface TradeJournal {
  id: number;
  symbol: string;
  direction: 'BUY' | 'SELL';
  entryPrice: number;
  stopLoss: number;
  takeProfit: number;
  timeframe: string;
  setupType: string;
  notes: string;
  screenshotPath: string;
  result: 'OPEN' | 'WIN' | 'LOSS' | 'BREAKEVEN';
  pnl?: number;
  rMultiple?: number;
  entryTime: string;
  exitTime?: string;
  createdAt: string;
  updatedAt: string;
}