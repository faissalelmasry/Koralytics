export interface DrillTimelineDto {
  events: DrillTimelineEvent[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DrillTimelineEvent {
  date: string;
  title: string;
  description: string | null;
  sessionId: number;
  sessionType: string;
  drillCategoryName: string | null;
  finalScore: number | null;
  drillNotes: string | null;
  drillTemplateName: string | null;
}
