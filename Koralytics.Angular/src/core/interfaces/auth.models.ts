import { User } from './user.model';

export interface TokenPair {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  userId: number;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AuthResultDto {
  user: AuthResponseDto;
  tokens: TokenPair;
}

export interface LoginRequest {
  emailOrUserName: string;
  password?: string; // Optional because we might not send it if OAuth
}

export interface OAuthLoginRequest {
  provider: string;
  idToken: string;
}

export interface OAuthLoginResult {
  requiresProfileCompletion: boolean;
  authResult?: AuthResultDto;
  userId?: number;
  temporaryToken?: string;
}

// Password DTOs
export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// Registration DTOs
export interface BaseRegistrationRequest {
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password?: string;
  confirmPassword?: string;
  phoneNumber?: string;
  profileImageUrl?: string;
}

export interface RegisterPlayerRequest extends BaseRegistrationRequest {
  dateOfBirth: string; // ISO 8601 string
  nationality?: string;
  preferredFoot: string;
  weakFootRating: number;
  playStyleTag?: string;
  archetypePlayerName?: string;
  archetypeText?: string;
}

export interface RegisterParentRequest extends BaseRegistrationRequest {
  childPlayerId: number;
}

export interface RegisterCoachRequest extends BaseRegistrationRequest {}
export interface RegisterScouterRequest extends BaseRegistrationRequest {}
export interface RegisterAcademyAdminRequest extends BaseRegistrationRequest {}

// Profile Completion DTOs
export interface CompleteProfileBase {
  userName?: string;
  phoneNumber?: string;
}

export interface CompleteProfileAsPlayer extends CompleteProfileBase {
  dateOfBirth?: string;
  nationality?: string;
  preferredFoot?: string;
  weakFootRating?: number;
}

export interface CompleteProfileAsParent extends CompleteProfileBase {
  childPlayerId: number;
}

export interface CompleteProfileAsCoach extends CompleteProfileBase {}
export interface CompleteProfileAsScouter extends CompleteProfileBase {}
export interface SendEmailConfirmation {
  userId: number;
}

export interface ConfirmEmail {
  userId: number;
  token: string;
}
