import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';

interface AnalysisData {
  id: string;
  parcelId: string;
  parcelName: string;
  status: 'Completed' | 'Running' | 'Pending' | 'Failed';
  modulesCompleted: string[];
  totalModules: number;
  createdAt: string;
}

@Component({
  selector: 'app-analysis-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, StatCardComponent],
  templateUrl: './analysis-list.component.html',
  styleUrls: ['./analysis-list.component.css'],
})
export class AnalysisListComponent {
  analyses = signal<AnalysisData[]>([]);
  searchTerm = signal('');
  isLoading = signal(true);

  readonly allModules = ['topography', 'soil', 'risk', 'bearing', 'boreholes', 'report'];

  // ── Summary KPIs ──
  totalAnalyses = computed(() => this.analyses().length);
  completedAnalyses = computed(
    () => this.analyses().filter((a) => a.status === 'Completed').length,
  );
  runningAnalyses = computed(() => this.analyses().filter((a) => a.status === 'Running').length);
  failedAnalyses = computed(() => this.analyses().filter((a) => a.status === 'Failed').length);

  filteredAnalyses = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.analyses();
    return this.analyses().filter(
      (a) => a.parcelName.toLowerCase().includes(term) || a.parcelId.toLowerCase().includes(term),
    );
  });

  constructor() {
    this.loadAnalyses();
  }

  private loadAnalyses() {
    // Simulate API call - replace with real backend data
    const mockData: AnalysisData[] = [
      {
        id: 'ana_001',
        parcelId: 'parcel_550e8400',
        parcelName: 'Talaat Moustafa Group',
        status: 'Completed',
        modulesCompleted: ['topography', 'soil', 'risk'],
        totalModules: this.allModules.length,
        createdAt: '2026-05-25T01:38:00Z',
      },
      {
        id: 'ana_002',
        parcelId: 'parcel_1a2b3c4d',
        parcelName: 'Orascom Construction',
        status: 'Running',
        modulesCompleted: ['topography', 'soil'],
        totalModules: this.allModules.length,
        createdAt: '2026-05-20T10:00:00Z',
      },
      {
        id: 'ana_003',
        parcelId: 'parcel_9z8y7x6w',
        parcelName: 'Egyptian Resorts Company',
        status: 'Pending',
        modulesCompleted: [],
        totalModules: this.allModules.length,
        createdAt: '2026-05-28T08:30:00Z',
      },
      {
        id: 'ana_004',
        parcelId: 'parcel_xxx',
        parcelName: 'Cairo Site 12',
        status: 'Failed',
        modulesCompleted: ['topography'],
        totalModules: this.allModules.length,
        createdAt: '2026-05-19T14:00:00Z',
      },
      {
        id: 'ana_005',
        parcelId: 'parcel_yyy',
        parcelName: 'Alex West Marina 7',
        status: 'Completed',
        modulesCompleted: ['topography', 'soil', 'risk', 'bearing'],
        totalModules: this.allModules.length,
        createdAt: '2026-05-22T09:15:00Z',
      },
    ];

    this.analyses.set(mockData);
    this.isLoading.set(false);
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
}
