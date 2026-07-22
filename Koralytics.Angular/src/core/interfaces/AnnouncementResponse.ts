export type AnnouncementTargetTypeName = 'All' | 'Team' | 'AgeGroup' | 'Role';
 
export interface AnnouncementResponseDto {
  id: number;
  academyId: number;
  title: string;
  body: string;
  targetType: AnnouncementTargetTypeName;
  targetId: number;
  sentByUserId: number;
  sentByFullName: string;
  createdAt: string;
}