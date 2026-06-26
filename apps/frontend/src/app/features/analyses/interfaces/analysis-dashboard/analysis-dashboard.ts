interface AnalysisJobsSummaryResponse {
  total: number;
  completed: number;
  running: number;
  failed: number;
  analysis: AnalysisJobSummaryItem[];
}

interface AnalysisJobSummaryItem {
  id: string;
  name: string;
  status: string;
  modules: string[];
  date: string;
}
