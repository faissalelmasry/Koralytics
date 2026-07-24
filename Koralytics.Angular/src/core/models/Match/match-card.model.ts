export interface MatchCardModel {
  id: number;
  homeTeamName: string;
  awayTeamName: string;
  homeTeamAcademyName?: string;
  awayTeamAcademyName?: string;
  homeTeamId: number;
  awayTeamId: number;
  type: string;
  format: string;
  matchDate: string;
  location: string;
  status: string;
  homeScore: number;
  awayScore: number;
  homePenaltyScore?: number;
  awayPenaltyScore?: number;
  winningTeamId?: number;
  coachOutcome?: 'win' | 'loss' | 'draw';
  coachSide?: 'home' | 'away';
}
