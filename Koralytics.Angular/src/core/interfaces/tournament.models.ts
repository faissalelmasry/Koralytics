export enum MatchFormat {
  FiveSide = 'FiveSide',
  SevenSide = 'SevenSide',
  ElevenSide = 'ElevenSide'
}

export enum TournamentStructure {
  Knockout = 'Knockout',
  GroupAndKnockout = 'GroupAndKnockout',
  League = 'League'
}

export enum TournamentStatus {
  Draft = 'Draft',
  Registration = 'Registration',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export enum TournamentTeamStatus {
  Invited = 'Invited',
  Accepted = 'Accepted',
  Rejected = 'Rejected'
}

export interface Tournament {
  id: number;
  name: string;
  format: MatchFormat;
  structure: TournamentStructure;
  ageGroupName: string;
  hasTwoLegs: boolean;
  startDate: string;
  endDate: string;
  status: TournamentStatus;
  entryFee: number;
}

export interface CreateTournamentDto {
  name: string;
  format: MatchFormat;
  structure: TournamentStructure;
  ageGroupId: number;
  hasTwoLegs: boolean;
  startDate: string;
  endDate: string;
  entryFee: number;
}

export interface BracketDto {
  rounds: TournamentRoundDto[];
}

export interface TournamentRoundDto {
  id: number;
  name: string;
  roundNumber: number;
  fixtures: TournamentFixtureDto[];
}

export interface TournamentFixtureDto {
  id: number;
  homeTeamName: string;
  awayTeamName: string;
  homeScore?: number;
  awayScore?: number;
  matchDate?: string;
  status: string;
}

export interface HallOfFameDto {
  playerId: number;
  playerName: string;
  awardType: string;
  teamName: string;
}

export interface AIReportDto {
  tournamentId: number;
  reportText: string;
  isPending: boolean;
  status?: string;
  generatedAt?: string;

  winnerTeamName?: string;
  winnerTeamId?: number;
  bestPlayerName?: string;
  bestPlayerId?: number;
  topScorerName?: string;
  topScorerId?: number;
  topScorerGoals?: number;
  topAssisterName?: string;
  topAssisterId?: number;
  topAssisterAssists?: number;
  mostScoredClubName?: string;
  mostScoredClubGoals?: number;
  mostConcededClubName?: string;
  mostConcededClubGoals?: number;
  leastScoredClubName?: string;
  leastScoredClubGoals?: number;
}

export interface GoalEventDto {
  playerId: number;
  assistPlayerId?: number;
  minute: number;
  isHomeSide: boolean;
}

export interface UpdateFixtureStatsDto {
  goals: GoalEventDto[];
  motmPlayerId?: number;
}
