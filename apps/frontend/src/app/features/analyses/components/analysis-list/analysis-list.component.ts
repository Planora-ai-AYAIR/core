import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';
import { AnalysisListFacadeService } from '../../services/analysis-dashboard/analysis-list-facade.service.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-analysis-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, StatCardComponent],
  templateUrl: './analysis-list.component.html',
  styleUrls: ['./analysis-list.component.css'],
})
export class AnalysisListComponent {
  private readonly analysisDashboard = inject(AnalysisListFacadeService);
  private readonly destroyRef = inject(DestroyRef);
  private router = inject(Router);

  analyses = signal<AnalysisJobSummaryItem[]>([]);
  searchTerm = signal('');
  isLoading = signal(true);
  error = signal<string | null>(null);

  readonly allModules = ['topography', 'soil', 'risk', 'bearing', 'boreholes', 'report'];

  totalAnalyses = signal(0);
  completedAnalyses = signal(0);
  runningAnalyses = signal(0);
  failedAnalyses = signal(0);

  filteredAnalyses = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.analyses();
    return this.analyses().filter(
      (a) => a.name.toLowerCase().includes(term) || a.id.toLowerCase().includes(term),
    );
  });

  constructor() {
    this.loadAnalyses();
  }

  private loadAnalyses() {
    this.analysisDashboard
      .getAnalysisDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isLoading.set(false);
          if (response) {
            this.analyses.set([...response.analysis]);
            this.totalAnalyses.set(response.total);
            this.completedAnalyses.set(response.completed);
            this.runningAnalyses.set(response.running);
            this.failedAnalyses.set(response.failed);
          }
        },
        error: () => {
          this.isLoading.set(false);
          this.error.set('Failed to load analyses. Please try again.'); // <-- set error
        },
      });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bg-planora-silt-100 text-planora-silt-700';
      case 'Running':
        return 'bg-planora-gold-100 text-planora-gold-700';
      case 'Failed':
        return 'bg-planora-risk-100 text-planora-risk-700';
      default:
        return 'bg-planora-desert-100 text-planora-basalt-700';
    }
  }

  viewAnalysis(analysis: AnalysisJobSummaryItem): void {
    this.router.navigate(['/app/analyses', analysis.id, analysis.parcelId]);
  }
}
