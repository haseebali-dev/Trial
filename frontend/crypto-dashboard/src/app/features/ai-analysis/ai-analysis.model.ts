export interface AIAnalysis {
  id: number;
  symbol: string;
  modelName: string;
  decision: 'BUY' | 'SELL' | 'WAIT';
  confidence: number;
  marketCondition: 'TRENDING' | 'RANGING' | 'VOLATILE' | 'UNCLEAR';
  reasons: string[];
  risks: string[];
  entryValid: boolean;
  stopLossValid: boolean;
  riskRewardValid: boolean;
  summary: string;
  timestamp: string;
}

export interface AIConsensus {
  id: number;
  symbol: string;
  consensusDecision: 'BUY' | 'SELL' | 'WAIT';
  agreeCount: number;
  totalModels: number;
  modelDecisions: Array<{ model: string; decision: string }>;
  timestamp: string;
}