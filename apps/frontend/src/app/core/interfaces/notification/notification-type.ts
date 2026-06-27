export type NotificationType =
  | 'ReportQueued'
  | 'ReportProcessing'
  | 'ReportCompleted'
  | 'ReportFailed'
  | 'PaymentReceived'
  | 'PaymentRefunded'
  | 'AccountBanned'
  | 'AccountActivated'
  | 'ModuleCompleted'
  | 'ModuleFailed'
  | 'AnalysisStarted';
