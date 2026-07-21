
export interface CreateAnnouncementPayload {
  title: string;
  body: string;
  targetType: number; // see AnnouncementTargetType enum
  targetId: number; // required (> 0) when targetType is Team or AgeGroup, otherwise 0
  role: string; // required when targetType is Role (e.g. "Coach" | "Player" | "Parent"), otherwise ''
}