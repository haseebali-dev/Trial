export interface WatchlistItem {
  id: number;
  symbol: string;
  name: string;
  isFavorite: boolean;
  defaultTimeframe: string;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}