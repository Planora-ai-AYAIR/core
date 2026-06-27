export interface ParcelAnalysisFullResponse {
  pythonJobId: string;
  backendJobId: string;
  parcelId: string;
  status: string;
  startedAt?: string;
  completedAt?: string;
  processingTimeSeconds: number;
  result: any;
  presignedUrlsExpireAt: string;
}
