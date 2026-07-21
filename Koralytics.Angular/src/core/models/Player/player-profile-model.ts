import { PlayerCardModel } from './player-card-model';

export interface PlayerProfileModel {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  age: number;
  nationality: string | null;
  archetypeText: string | null;
  availabilityStatus: number;

  positions: PlayerPositionModel[];
  currentAcademy: PlayerAcademyModel | null;
  teams: PlayerTeamModel[];

  playerCard: PlayerCardModel | null;

  totalMatches: number;
  totalGoals: number;
  totalAssists: number;
  totalMOTMs: number;

  sessionStats: MatchTypeStatsModel;
  friendlyStats: MatchTypeStatsModel;
  tournamentStats: MatchTypeStatsModel;
}

export interface PlayerPositionModel {
  position: string;
  isPrimary: boolean;
}

export interface PlayerAcademyModel {
  academyId: number;
  academyName: string;
  joinedAt: string;
}

export interface PlayerTeamModel {
  teamId: number;
  teamName: string;
  ageGroupName: string | null;
}

export interface MatchTypeStatsModel {
  matches: number;
  goals: number;
  assists: number;
  motMs: number;
}
