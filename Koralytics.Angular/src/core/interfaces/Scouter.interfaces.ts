

export interface ApiEnvelope<T> {
  message: string;
  data: T;
}

export interface GenerateReportResponse {
  message: string;
  report: string;
}

export interface MessageOnlyResponse {
  message: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface PlayerCardDto {
  playerId: number;
  playerName: string;
  position: string;
  overallRating: number;
  paceRating?: number;
  dribblingRating?: number;
  shootingRating?: number;
  defendingRating?: number;
  passingRating?: number;
  physicalRating?: number;
  goalkeepingRating?: number;
  transferClassification: string;
  archetypePlayerName?: string;
  playStyleTag?: string;
  preferredFoot: number;
  weakFootRating: number;
  profileImageUrl?: string;
}

export interface PlayerSearchFiltersDto {
  minAge?: number;
  maxAge?: number;
  preferredFoot?: number;
  positions?: string[];
  academyId?: number;
  format?: string;
  minRating?: number;
  maxRating?: number;
  pageNumber: number;
  pageSize: number;
}

export interface ScouterProfileDto {
  id: number;
  fullName: string;
  isVerified: boolean;
  verifiedAt?: string;
  [key: string]: unknown;
}

export interface ScouterShortlistDto {
  id: number;
  scouterUserId: number;
  playerId: number;
  addedAt: string;
}

export interface ProfileViewerDetailDto {
  scouterId: number;
  scouterName: string;
  isScouterVerified: boolean;
  viewedAt: string;
}

export interface PlayerProfileViewAnalyticsDto {
  totalViewsCount: number;
  recentViews: ProfileViewerDetailDto[];
}

export interface ScouterReport {
  id: number;
  scouterUserId: number;
  playerId: number;
  content?: string;
  createdAt?: string;
}