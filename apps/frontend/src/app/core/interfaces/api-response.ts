import { ApiError } from './api-error';

export interface ApiResponse<T> {
  statusCode: number;
  message: string;
  errors: ApiError[] | null;
  data: T | null;
}
