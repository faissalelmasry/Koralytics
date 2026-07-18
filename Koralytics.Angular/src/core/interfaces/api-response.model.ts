export interface ApiResponse<T> {
  isSuccess: boolean;
  statusCode: number;
  message: string;
  data?: T;
  errorCode?: string;
  errors?: any;
}
