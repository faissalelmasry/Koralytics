import { MatchCardModel } from './match-card.model';

export interface MatchListDto {
  matches: MatchCardModel[];
  coachTeamIds: number[];
  academyTeamIds: number[];
  totalCount: number;
  page: number;
  pageSize: number;
}
