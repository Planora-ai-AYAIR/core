export interface StartAnalysisResponse {
  analysisJobId: string;
  parcelId: string;
  status: string;
  submittedAt: string;
  estimatedDuration: string;
  pollEndpoint: string;
}
