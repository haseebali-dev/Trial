export interface Signal {
  id: number;
  symbol: string;
  direction: 'BUY' | 'SELL' | 'WAIT';
  score: number;
  maxScore: number;
  confidenceLevel: 'HIGH' | 'MEDIUM' | 'LOW';
  entryPrice?: number;
  stopLoss?: number;
  takeProfit1?: number;
  takeProfit2?: number;
  takeProfit3?: number;
  riskReward?: number;
  reasons: string[];
  timestamp: string;
}