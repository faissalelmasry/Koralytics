export interface TeamScheduledEventsResponseDto {
  events: TeamScheduledEventDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TeamScheduledEventDto {
  eventType: string;
  date: string;
  matchId: number | null;
  matchType: string | null;
  homeTeamName: string | null;
  awayTeamName: string | null;
  sessionId: number | null;
  sessionType: string | null;
  teamId: number;
  teamName: string;
  notes: string | null;
}
