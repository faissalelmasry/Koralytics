// ── Match Request & Analytics DTOs ──
// Matched 1:1 against C# DTOs in Koralytics.Application/DTOs/Match/

export interface CreateMatchRequestDto {
  requesterTeamId: number;
  targetTeamId: number;
  format: string;
  proposedDate: string;
  location?: string;
}

export interface MatchRequestResponseDto {
  id: number;
  requesterTeamId: number;
  requesterTeamName: string;
  targetTeamId: number;
  targetTeamName: string;
  requesterCoachId: number;
  requesterCoachName: string;
  format: string;
  proposedDate: string;
  location?: string;
  status: string;
  resolvedByCoachId?: number;
  resolvedByCoachName?: string;
  resolvedAt?: string;
  matchId?: number;
  createdAt: string;
}

export interface PlayerReadinessDto {
  playerId: number;
  playerName: string;
  readinessScore: number;
  status: string;
  matchesPlayedLast7Days: number;
  recommendation: string;
  availabilityStatus?: string;
  lastSessionScores?: number[];
}
