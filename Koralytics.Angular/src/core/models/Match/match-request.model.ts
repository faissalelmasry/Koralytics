export interface MatchRequestModel {
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

export interface MatchRequestListDto {
  requests: MatchRequestModel[];
  totalCount: number;
  page: number;
  pageSize: number;
}
