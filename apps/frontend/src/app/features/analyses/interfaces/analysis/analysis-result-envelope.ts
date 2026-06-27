export interface AnalysisResultEnvelope {
  EventType: string;
  ParcelId: string;
  AnalysisJobId: string;
  AnalysisType: string;
  Result: any;
  Timestamp: string;
}
