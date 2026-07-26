import { User } from './user.model';

export interface AcademyResponseDto {
  id: number;
  name: string;
  logoUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  createdAt: string;
  locationCount: number;
}

export interface CreateAcademyRequestDto {
  academyName: string;
  contactPersonName: string;
  contactEmail: string;
  contactPhone: string;
  location: string;
}

export interface AcademyRequestResponseDto {
  id: number;
  academyName: string;
  status: number;
  requestedAt: string;
  requestedById: number;
  requestedByFullName: string;
  rejectedReason?: string;
}

export enum AcademyBadgeType {
  Verified = 1,
  TopPerformer = 2,
  Premium = 3
}

export interface AcademyBadgeResponseDto {
  id: number;
  academyId: number;
  badgeType: AcademyBadgeType;
  awardedAt: string;
  createdAt: string;
}

export interface AcademyMemberResponseDto {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  position?: string;
  squadStatus?: string;
  joinedAt: string;
  subscriptionStatus?: number;
  graceUntil?: string;
}

export interface AcademyAdminResponseDto {
  userId: number;
  fullName: string;
  email: string;
  isOwner: boolean;
}

export interface UpdatePlayerSubscriptionDto {
  status: number; // 0=Unpaid, 1=Paid, 2=GracePeriod
  graceUntil?: string;
}

export interface PaginationRequestDto {
  pageNumber?: number;
  pageSize?: number;
}

export interface PagedResponseDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateAgeGroupDto { name: string; minAge: number; maxAge: number; }
export interface AgeGroupResponseDto { id: number; academyId: number; name: string; minAge: number; maxAge: number; }
export interface CreateTeamDto { name: string; ageGroupId: number; locationId: number; }
export interface TeamResponseDto { id: number; academyId: number; name: string; ageGroupId: number; ageGroupName: string; locationId: number; locationName: string; coaches: any[]; players: any[]; }
export interface AcademyLocationResponseDto { id: number; name: string; address: string; city: string; country: string; isMainLocation: boolean; }

export interface CreateAnnouncementDto {
  targetAudience: number; // 0=Everyone, 1=Coaches, 2=Players, 3=Parents
  title: string;
  message: string;
}

export interface AnnouncementResponseDto {
  id: number;
  academyId: number;
  targetAudience: number;
  title: string;
  message: string;
  sentAt: string;
  sentById: number;
  sentByFullName: string;
}

export interface SubscriptionStatusResponseDto {
  playerId: number;
  playerFullName: string;
  status: number; // 0=Unpaid, 1=Paid, 2=GracePeriod
  graceUntil?: string;
}
