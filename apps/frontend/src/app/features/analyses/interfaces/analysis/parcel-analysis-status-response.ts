import { ModuleStatusDto } from "./module-status-dto";

export interface ParcelAnalysisStatusResponse {
  parcelId: string;
  status: string;
  modules: ModuleStatusDto[];
  updatedAt?: string;
}
