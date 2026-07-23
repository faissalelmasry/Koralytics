export interface ProfileViewsResponse {
  message: string;
  data: PlayerProfileViewAnalyticsDto;
}

export interface PlayerProfileViewAnalyticsDto {
  totalViewsCount: number;
  recentViews: ProfileViewerDetailDto[];
}

export interface ProfileViewerDetailDto {
  scouterId: number;
  scouterName: string;
  isScouterVerified: boolean;
  viewedAt: string;
}
