// ── Coach DTOs ──
// Matched 1:1 against C# DTOs in Koralytics.Application/DTOs/Coach/

export interface SquadPlayerDto {
  playerId: number;
  fullName: string;
  profileImageUrl?: string;
  primaryPosition: string;
  availabilityStatus: string;
  overallRating: number;
  paceRating: number;
  dribblingRating: number;
  shootingRating: number;
  defendingRating: number;
  passingRating: number;
  physicalRating: number;
  goalkeepingRating?: number;
  preferredFoot: string;
  weakFootRating: number;
  archetypePlayerName?: string;
  playStyleTag?: string;
}

export interface SquadOverviewDto {
  teamId: number;
  teamName: string;
  players: SquadPlayerDto[];
}

export interface TrainingTeamSplitDto {
  sessionId: number;
  teamA: SquadPlayerDto[];
  teamB: SquadPlayerDto[];
}

export interface SquadComparisonDto {
  playerA: SquadPlayerDto;
  playerB: SquadPlayerDto;
}

export interface WriteNoteDto {
  playerId: number;
  note: string;
  isPublic: boolean;
  sessionId?: number;
  matchId?: number;
}

export interface CoachNoteDto {
  id: number;
  playerId: number;
  playerFullName: string;
  note: string;
  isPublic: boolean;
  sessionId?: number;
  matchId?: number;
  createdAt: string;
}

export interface GrantTempAccessDto {
  grantedToUserId: number;
  accessLevel: 'ReadOnly' | 'FullSquad';
  expiresAt: string;
}

export interface TempAccessDto {
  id: number;
  coachUserId: number;
  grantedToUserId: number;
  grantedToFullName: string;
  accessLevel: 'ReadOnly' | 'FullSquad';
  status: 'Active' | 'Revoked' | 'Expired';
  expiresAt: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
