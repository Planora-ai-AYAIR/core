import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ROUTES } from '../../../../shared/config/constants';
import { ParcelFacadeService } from '../../../parcels/services/parcel-facade.service';
import { AnalysisListFacadeService } from '../../../analyses/services/analysis-dashboard/analysis-list-facade.service.service';
import { ParcelListResponse } from '../../../parcels/interfaces/parcel-list/parcel-list-response';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-home.component.html',
  styleUrls: ['./dashboard-home.component.css'],
})
export class DashboardHomeComponent implements OnInit {
  private router = inject(Router);
  private parcelFacade = inject(ParcelFacadeService);
  private analysisFacade = inject(AnalysisListFacadeService);

  parcels = signal<ParcelListResponse[]>([]);
  analysisSummary = signal<{
    total: number;
    completed: number;
    running: number;
    failed: number;
    analysis: AnalysisJobSummaryItem[];
  } | null>(null);

  loading = signal(true);
  error = signal<string | null>(null);

  // ── Summary metrics ──
  metrics = computed(() => [
    { label: 'Registered Parcels', value: this.parcels().length.toString(), icon: 'pi-map' },
    {
      label: 'Total Area',
      value: `${this.parcels()
        .reduce((sum, p) => sum + p.areaHectares, 0)
        .toFixed(1)} ha`,
      icon: 'pi-chart-line',
    },
    {
      label: 'Ongoing Analyses',
      value: this.analysisSummary()?.running.toString() ?? '0',
      icon: 'pi-spin pi-spinner',
    },
    {
      label: 'Completed Analyses',
      value: this.analysisSummary()?.completed.toString() ?? '0',
      icon: 'pi-check-circle',
    },
  ]);

  // ── Recent parcels (first 5) ──
  recentParcels = computed(() => this.parcels().slice(0, 5));

  // ── Recent analyses (all statuses, first 5) ──
  recentAnalyses = computed(() => (this.analysisSummary()?.analysis ?? []).slice(0, 5));

  ngOnInit(): void {
    this.loadData();
  }

  private loadData() {
    this.loading.set(true);
    let parcelDone = false;
    let analysisDone = false;

    this.parcelFacade.getMyParcels().subscribe({
      next: (parcels) => {
        this.parcels.set(parcels ?? []);
        parcelDone = true;
        if (parcelDone && analysisDone) this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parcels');
        this.loading.set(false);
      },
    });

    this.analysisFacade.getAnalysisDashboard().subscribe({
      next: (data) => {
        this.analysisSummary.set(data);
        analysisDone = true;
        if (parcelDone && analysisDone) this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load analysis summary');
        this.loading.set(false);
      },
    });
  }

  // ── Navigation ──
  newParcel() {
    this.router.navigate([ROUTES.newParcel]);
  }

  viewAllParcels() {
    this.router.navigate(['/app/parcels']);
  }

  viewAllAnalyses() {
    this.router.navigate(['/app/analyses']);
  }

  viewParcel(parcel: ParcelListResponse) {
    this.router.navigate(['/app/parcels', parcel.id]);
  }

  viewAnalysis(analysis: AnalysisJobSummaryItem) {
    this.router.navigate(['/app/analyses', analysis.id, analysis.parcelId]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bg-green-100 text-green-700';
      case 'Running':
        return 'bg-amber-100 text-amber-700';
      case 'Failed':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  }
}
