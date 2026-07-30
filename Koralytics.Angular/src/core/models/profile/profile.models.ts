export interface PlayerPositionDto {
  position: string;
  isPrimary: boolean;
}

export interface BaseUserProfileResponse {
  id: number;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  phoneNumber: string | null;
  profileImageUrl: string | null;
  role: string;
  createdAt: string;
}

export interface PlayerProfileResponse extends BaseUserProfileResponse {
  dateOfBirth: string | null;
  age: number | null;
  nationality: string | null;
  preferredFoot: string | number | null; // "Right" | "Left" | "Both" | 1 | 2 | 3
  weakFootRating: number | null; // 1 - 5
  heightCm: number | null;
  weightKg: number | null;
  playStyleTag: string | null;
  archetypePlayerName: string | null;
  archetypeText: string | null;
  availabilityStatus: string | number | null;
  positions: PlayerPositionDto[];
}

export interface ScouterProfileResponse extends BaseUserProfileResponse {
  isVerified: boolean | null;
  verifiedAt: string | null;
}

export interface AcademyAdminProfileResponse extends BaseUserProfileResponse {
  academyId: number | null;
  academyName: string | null;
}

export interface CoachProfileResponse extends BaseUserProfileResponse {}
export interface ParentProfileResponse extends BaseUserProfileResponse {}
export interface SystemAdminProfileResponse extends BaseUserProfileResponse {}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  nationality?: string | null;
  preferredFoot?: string | number | null;
  weakFootRating?: number | null;
  heightCm?: number | null;
  weightKg?: number | null;
  playStyleTag?: string | null;
  positions?: PlayerPositionDto[];
}
