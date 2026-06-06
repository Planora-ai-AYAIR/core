import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// ── Matching the backend response ──
interface BoundingBox {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

interface ParcelDetail {
  parcelId: string;
  clientName: string;
  area: number; // m²
  status: string; // e.g. "completed", "pending", "processing"
  modulesCompleted: string[];
  boundingBox: BoundingBox;
  createdAt: string;
  completedAt: string | null;
}

@Component({
  selector: 'app-parcel-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './parcel-detail.component.html',
  styleUrls: ['./parcel-detail.component.css'],
})
export class ParcelDetailComponent implements OnInit {
  parcel = signal<ParcelDetail | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);

  // Predefined list of all possible modules (used for progress display)
  readonly allModules = ['topography', 'soil', 'risk', 'bearing', 'boreholes', 'report'];

  ngOnInit(): void {
    // Simulate API call – replace with HttpClient when backend is ready
    setTimeout(() => {
      try {
        // Sample data matching the documented response
        this.parcel.set({
          parcelId: 'parcel_550e8400',
          clientName: 'Talaat Moustafa Group',
          area: 50000,
          status: 'completed',
          modulesCompleted: ['topography', 'soil', 'risk'],
          boundingBox: { minX: 30.0, minY: 31.0, maxX: 30.1, maxY: 31.1 },
          createdAt: '2026-05-25T01:38:00Z',
          completedAt: '2026-05-25T04:15:00Z',
        });
        this.isLoading.set(false);
      } catch (err) {
        this.error.set('Failed to load parcel data.');
        this.isLoading.set(false);
      }
    }, 800);
  }

  // Utility to format date strings
  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  // Status badge class mapping
  statusClass(status: string): string {
    switch (status) {
      case 'completed':
        return 'bg-planora-silt-100 text-planora-silt-700';
      case 'processing':
        return 'bg-planora-gold-100 text-planora-gold-700';
      case 'pending':
        return 'bg-planora-desert-100 text-planora-basalt-700';
      default:
        return 'bg-planora-desert-100 text-planora-basalt-700';
    }
  }

  exportReport() {
    // Placeholder for future report generation
    console.log('Generating report for:', this.parcel()?.parcelId);
  }
}
