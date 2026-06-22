export interface ApiError {
  field: string | null;
  code: string;
  message: string;
  metaData: Record<string, unknown> | null;
}
