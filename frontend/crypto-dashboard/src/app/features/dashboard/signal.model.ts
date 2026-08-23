export interface SignalDto {
  symbol: string;
  timeframe: string;
  type: string;
  direction: string;
  confidence: number;
  entryPrice: number;
  stopLoss?: number;
  takeProfit?: number;
  strategy: string;
  reason: string;
  metadata: Record<string, any>;
  timestamp: number;
}

export interface SignalSummaryDto {
  symbol: string;
  timeframe: string;
  overallScore: number;
  overallDirection: string;
  signals: SignalDto[];
  strategyScores: Record<string, number>;
  timestamp: number;
}

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