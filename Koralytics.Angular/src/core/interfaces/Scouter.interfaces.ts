// ── DTOs still not provided (PlayerCardDto, ScouterProfileDto,
// ScouterShortlistDto, PlayerProfileViewAnalyticsDto, ScouterReport) ──
// Shapes below remain inferred from field usage in the C# services. Replace
// with the real DTOs when available.

export interface ApiEnvelope<T> {
  message: string;
  data: T;
}

// GenerateScoutingReport uniquely returns `report` instead of `data` --
// see the controller review. Kept as a separate type so the mismatch is
// visible in the type system rather than silently handled.
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
  firstName: string;
  lastName: string;
  overallRating: number;
  [key: string]: unknown;
}

export interface PlayerSearchFiltersDto {
  minAge?: number;
  maxAge?: number;
  preferredFoot?: string;
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

// Confirmed against a real API response (2026-07-21):
// { scouterId, scouterName, isScouterVerified, viewedAt }
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

// GetScoutingReportAsync returns the raw entity server-side (see review) --
// shape here is a best guess from field names used elsewhere.
export interface ScouterReport {
  id: number;
  scouterUserId: number;
  playerId: number;
  content?: string;
  createdAt?: string;
}