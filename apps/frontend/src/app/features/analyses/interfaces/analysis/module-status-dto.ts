export interface ModuleStatusDto {
  type: string;
  status: string;
  errorMessage?: string | null;
  completedAt?: string;
}
