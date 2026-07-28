export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
}

export enum DrillMode {
  Manual = 'Manual',
  SuccessOrMissed = 'SuccessOrMissed',
}
export enum SessionStatus {
  Scheduled = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export enum SessionType {
  PreSeason = 1,
  Regular = 2,
  OffSeason = 3,
  SessionMatch = 4
}
export enum SubscriptionStatus {
  Paid = 'Paid',
  Unpaid = 'Unpaid',
  Grace = 'Grace'
}

export enum SubscriptionDuration {
  OneMonth = 'OneMonth',
  ThreeMonths = 'ThreeMonths',
  SixMonths = 'SixMonths',
  OneYear = 'OneYear'
}