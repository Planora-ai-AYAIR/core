import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';
import { ParcelFacadeService } from '../../services/parcel-facade.service';
import { ParcelListResponse } from '../../interfaces/parcel-list/parcel-list-response';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-parcel-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, StatCardComponent, ConfirmDialogComponent],
  templateUrl: './parcel-list.component.html',
  styleUrls: ['./parcel-list.component.css'],
})
export class ParcelListComponent implements OnInit {
  private parcelFacade = inject(ParcelFacadeService);

  parcels = signal<ParcelListResponse[]>([]);
  searchTerm = signal('');
  isLoading = signal(true);
  error = signal<string | null>(null);

  // Delete confirmation state
  showDeleteConfirm = signal(false);
  parcelToDelete = signal<string | null>(null);

  // ── Summary KPIs ──
  totalParcels = computed(() => this.parcels().length);
  totalAreaM2 = computed(() => this.parcels().reduce((acc, p) => acc + p.areaHectares * 10000, 0));
  processingCount = computed(() => this.parcels().filter((p) => p.status === 'Processing').length);

  filteredParcels = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.parcels();
    return this.parcels().filter(
      (p) =>
        p.name.toLowerCase().includes(term) || this.getLocation(p).toLowerCase().includes(term),
    );
  });

  ngOnInit(): void {
    this.parcelFacade.getMyParcels().subscribe({
      next: (parcels) => {
        if (parcels) {
          this.parcels.set(parcels);
          this.error.set(null);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parcels. Please try again.');
        this.isLoading.set(false);
      },
    });
  }

  // ── Display helpers ──
  getLocation(p: ParcelListResponse): string {
    const lat = p.centroidLatitude;
    const lng = p.centroidLongitude;
    if (lat == null || lng == null) return '—';
    const latDir = lat >= 0 ? 'N' : 'S';
    const lngDir = lng >= 0 ? 'E' : 'W';
    return `${Math.abs(lat).toFixed(3)}°${latDir}, ${Math.abs(lng).toFixed(3)}°${lngDir}`;
  }

  getAreaM2(p: ParcelListResponse): number {
    return p.areaHectares * 10000;
  }

  getCreated(p: ParcelListResponse): string {
    return new Date(p.createdAt).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Draft':
        return 'bg-planora-desert-100 text-planora-basalt-700';
      case 'Processing':
        return 'bg-planora-gold-100 text-planora-gold-700';
      case 'Completed':
        return 'bg-green-100 text-green-700';
      default:
        return 'bg-planora-desert-100 text-planora-basalt-700';
    }
  }

  getRiskProfile(p: ParcelListResponse): 'Low' | 'Medium' | 'High' {
    if (p.status === 'Processing') return 'Medium';
    if (p.status === 'Draft' || p.status === 'Completed') return 'Low';
    return 'Low';
  }

  // ── Delete actions ──
  requestDelete(parcelId: string): void {
    this.parcelToDelete.set(parcelId);
    this.showDeleteConfirm.set(true);
  }

  onDeleteConfirmed(): void {
    this.showDeleteConfirm.set(false);
    const parcelId = this.parcelToDelete();
    if (!parcelId) return;

    this.parcelFacade.deleteParcel(parcelId).subscribe((success) => {
      if (success) {
        this.parcels.update((list) => list.filter((p) => p.id !== parcelId));
      }
      this.parcelToDelete.set(null);
    });
  }

  onDeleteCancel(): void {
    this.showDeleteConfirm.set(false);
    this.parcelToDelete.set(null);
  }
}
