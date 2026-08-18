export interface Candle {
  timestamp: number;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
}

export interface MarketTicker {
  symbol: string;
  price: number;
  volume24h: number;
  change24h: number;
  timestamp: number;
  high24h?: number;
  low24h?: number;
  open24h?: number;
}

export interface MarketOverview {
  symbol: string;
  price: number;
  change24h: number;
  volume24h: number;
}

export type Timeframe = '1m' | '5m' | '15m' | '30m' | '1h' | '4h' | '1d';

export const TIMEFRAMES: Timeframe[] = ['1m', '5m', '15m', '30m', '1h', '4h', '1d'];

export const TIMEFRAME_LABELS: Record<Timeframe, string> = {
  '1m': '1 Minute',
  '5m': '5 Minutes',
  '15m': '15 Minutes',
  '30m': '30 Minutes',
  '1h': '1 Hour',
  '4h': '4 Hours',
  '1d': '1 Day'
};

// Technical Analysis Models
export interface SmaDto {
  timestamp: number;
  value: number;
}

export interface EmaDto {
  timestamp: number;
  value: number;
}

export interface RsiDto {
  timestamp: number;
  value: number;
}

export interface MacdDto {
  timestamp: number;
  macd: number;
  signal: number;
  histogram: number;
}

export interface BollingerBandsDto {
  timestamp: number;
  upper: number;
  middle: number;
  lower: number;
  percentB: number;
  bandwidth: number;
}

export interface AtrDto {
  timestamp: number;
  value: number;
}

export interface StochasticDto {
  timestamp: number;
  k: number;
  d: number;
}

export interface AdxDto {
  timestamp: number;
  adx: number;
  plusDi: number;
  minusDi: number;
}

export interface ObvDto {
  timestamp: number;
  value: number;
}

export interface VwapDto {
  timestamp: number;
  value: number;
}

export interface IndicatorsResponse {
  symbol: string;
  timeframe: string;
  sma20: SmaDto[];
  sma50: SmaDto[];
  sma200: SmaDto[];
  ema9: EmaDto[];
  ema21: EmaDto[];
  ema50: EmaDto[];
  rsi14: RsiDto[];
  macd: MacdDto[];
  bollingerBands20: BollingerBandsDto[];
  atr14: AtrDto[];
  stochastic14: StochasticDto[];
  adx14: AdxDto[];
  obv: ObvDto[];
  vwap: VwapDto[];
}

// Signal Models
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