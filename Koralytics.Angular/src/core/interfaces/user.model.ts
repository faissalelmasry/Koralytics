export interface User {
  userId: number;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
  academyId?: number;
}
