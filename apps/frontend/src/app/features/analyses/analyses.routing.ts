import { Routes } from '@angular/router';

export const ANALYSIS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/analysis-list/analysis-list.component').then((m) => m.AnalysisListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./components/analysis-new/analysis-new.component').then((m) => m.AnalysisNewComponent),
    title: 'New Analysis',
  },
  {
    path: ':analysisId/:parcelId',
    loadComponent: () =>
      import('./pages/analysis-detail/analysis-detail.component').then(
        (m) => m.AnalysisDetailComponent,
      ),
    title: 'Analysis Details',
  },
];
