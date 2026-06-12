import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';

// ── Backend matching interface ──
interface BoundingBox {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

interface ParcelData {
  parcelId: string;
  clientName: string;
  area: number; // m²
  status: string; // e.g. "completed", "pending", "processing"
  modulesCompleted: string[];
  boundingBox: BoundingBox;
  createdAt: string;
  completedAt: string | null;
}

// ── Internal display interface ──
interface GeotechParcel {
  id: string;
  name: string;
  location: string;
  areaM2: number; // area in square meters
  created: string;
  modulesCompletedCount: number; // number of completed analysis modules
  totalModules: number; // total possible modules (6)
  riskProfile: 'Low' | 'Medium' | 'High';
}

@Component({
  selector: 'app-parcel-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, StatCardComponent],
  templateUrl: './parcel-list.component.html',
  styleUrls: ['./parcel-list.component.css'],
})
export class ParcelListComponent {
  parcels = signal<GeotechParcel[]>([]);
  searchTerm = signal('');
  isLoading = signal(true);

  readonly totalModulesCount = 6; // from project: topography, soil, risk, bearing, boreholes, report

  // ── Summary KPIs ──
  totalParcels = computed(() => this.parcels().length);

  totalAreaM2 = computed(() => this.parcels().reduce((acc, p) => acc + p.areaM2, 0));

  totalModulesCompleted = computed(() =>
    this.parcels().reduce((acc, p) => acc + p.modulesCompletedCount, 0),
  );

  filteredParcels = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.parcels();
    return this.parcels().filter(
      (p) => p.name.toLowerCase().includes(term) || p.location.toLowerCase().includes(term),
    );
  });

  constructor() {
    this.loadParcels();
  }

  private loadParcels() {
    const raw: ParcelData[] = [
      {
        parcelId: 'parcel_550e8400',
        clientName: 'Talaat Moustafa Group',
        area: 50000,
        status: 'completed',
        modulesCompleted: ['topography', 'soil', 'risk'],
        boundingBox: { minX: 30.0, minY: 31.0, maxX: 30.1, maxY: 31.1 },
        createdAt: '2026-05-25T01:38:00Z',
        completedAt: '2026-05-25T04:15:00Z',
      },
      {
        parcelId: 'parcel_1a2b3c4d',
        clientName: 'Orascom Construction',
        area: 120000,
        status: 'processing',
        modulesCompleted: ['topography', 'soil'],
        boundingBox: { minX: 29.5, minY: 31.2, maxX: 29.6, maxY: 31.3 },
        createdAt: '2026-05-20T10:00:00Z',
        completedAt: null,
      },
      {
        parcelId: 'parcel_9z8y7x6w',
        clientName: 'Egyptian Resorts Company',
        area: 30000,
        status: 'pending',
        modulesCompleted: [],
        boundingBox: { minX: 28.0, minY: 32.0, maxX: 28.1, maxY: 32.1 },
        createdAt: '2026-05-28T08:30:00Z',
        completedAt: null,
      },
    ];

    const mapped: GeotechParcel[] = raw.map((p) => ({
      id: p.parcelId,
      name: p.clientName,
      location: `${p.boundingBox.minY.toFixed(3)}°N, ${p.boundingBox.minX.toFixed(3)}°E`,
      areaM2: p.area,
      created: new Date(p.createdAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      }),
      modulesCompletedCount: p.modulesCompleted.length,
      totalModules: this.totalModulesCount,
      riskProfile: this.mapStatusToRisk(p.status),
    }));

    this.parcels.set(mapped);
    this.isLoading.set(false);
  }

  private mapStatusToRisk(status: string): 'Low' | 'Medium' | 'High' {
    switch (status) {
      case 'completed':
        return 'Low';
      case 'processing':
        return 'Medium';
      default:
        return 'Low';
    }
  }
}
