export interface PlayerEventKPI {
  type: string;
  count: number;
}

export interface MiniPlayerCardModel {
  playerId: number;
  fullName: string;
  position: string;
  profileImageUrl: string | null;
  overallRating: number;
  matchEvents?: PlayerEventKPI[];
}
