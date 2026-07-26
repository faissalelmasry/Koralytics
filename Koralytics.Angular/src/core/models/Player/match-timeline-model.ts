export interface MatchTimelineDto {
  events: MatchTimelineEventModel[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface MatchTimelineEventModel {
  date: string;
  title: string;
  matchId: number;
  matchType: string;
  homeTeamName: string | null;
  awayTeamName: string | null;
  homeScore: number;
  awayScore: number;
  homePenaltyScore: number | null;
  awayPenaltyScore: number | null;
  goals: number;
  assists: number;
  minutesPlayed: number;
  isMOTM: boolean;
  rating: number | null;
  coachNote: string | null;
  description: string | null;
}
