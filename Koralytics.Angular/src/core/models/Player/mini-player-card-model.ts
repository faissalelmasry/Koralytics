export interface PlayerEventKPI {
  type: string;
  count: number;
}

export interface MiniPlayerCardModel {
  playerId: number;
  fullName: string;
  position: string;          // positionInMatch (role in this match, e.g. ST, CB)
  naturalPosition?: string;  // player's primary/natural position from profile
  profileImageUrl: string | null;
  overallRating: number;
  matchEvents?: PlayerEventKPI[];
}
