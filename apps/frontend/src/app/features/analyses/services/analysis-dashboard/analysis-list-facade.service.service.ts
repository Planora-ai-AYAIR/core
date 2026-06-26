import { inject, Injectable, signal } from '@angular/core';
import { AnalysisListApiService } from './analysis-list-api.service';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { ToastService } from '../../../../shared/services/toaster.service';

@Injectable({
  providedIn: 'root',
})
export class AnalysisListFacadeService {
  private apiService = inject(AnalysisListApiService);
  private toast = inject(ToastService);

  readonly error = signal<string | null>(null);

  getAnalysisDashboard(): Observable<AnalysisJobsSummaryResponse | null> {
    return this.apiService.getAnalysisDashboard().pipe(
      map((response) => response?.data ?? null),
      catchError((err) => {
        const envelope = err?.error;
        const message = envelope?.message ?? 'Failed to retrieve analysis dashboard';
        const errors: any[] = envelope?.errors ?? [];
        if (errors.length > 0) {
          errors.forEach((e) => {
            this.toast.error(e.message);
          });
        } else {
          this.toast.error(message);
        }
        this.error.set(message);
        return of(null);
      }),
    );
  }
}
