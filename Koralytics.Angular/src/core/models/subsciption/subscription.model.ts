import { SubscriptionStatus, SubscriptionDuration } from '../../enums/koralytics.enums';

export interface PlayerSubscriptionDto {
  id: number;
  playerId: number;
  playerName: string;
  academyId: number;
  academyName: string;
  academyTier?: string | number;
  amount: number;
  status: SubscriptionStatus;
  duration: SubscriptionDuration;
  startDate: string;
  dueDate: string;
  paidAt?: string;
  graceUntil?: string;
  paidByUserId?: number;
  paidByUserName?: string;
}

export interface CreateSubscriptionDto {
  playerId: number;
  academyId: number;
  amount: number;
  duration: SubscriptionDuration;
  startDate?: string;
}

export interface PaySubscriptionRequest {
  subscriptionId: number;
}

export interface PaymentIntentResponse {
  clientSecret: string;
  publishableKey: string;
}