import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ParcelFacadeService } from '../../services/parcel-facade.service';
import { ParcelDetailResponse } from '../../interfaces/parcel-detail/parcel-detail-response';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-parcel-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ConfirmDialogComponent],
  templateUrl: './parcel-detail.component.html',
  styleUrls: ['./parcel-detail.component.css'],
})
export class ParcelDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private parcelFacade = inject(ParcelFacadeService);

  parcel = signal<ParcelDetailResponse | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  showDeleteConfirm = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('No parcel ID provided');
      this.isLoading.set(false);
      return;
    }

    this.parcelFacade.getParcelById(id).subscribe({
      next: (data) => {
        if (data) {
          this.parcel.set(data);
        } else {
          this.error.set('Parcel not found');
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('An unexpected error occurred');
        this.isLoading.set(false);
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

  getLocationString(lat: number, lng: number): string {
    const latDir = lat >= 0 ? 'N' : 'S';
    const lngDir = lng >= 0 ? 'E' : 'W';
    return `${Math.abs(lat).toFixed(6)}°${latDir}, ${Math.abs(lng).toFixed(6)}°${lngDir}`;
  }

  statusClass(status: string): string {
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

  requestDelete(): void {
    this.showDeleteConfirm.set(true);
  }

  // Called when user clicks "Delete" in the dialog
  onDeleteConfirmed(): void {
    this.showDeleteConfirm.set(false);
    const parcelId = this.parcel()?.id;
    if (!parcelId) return;

    this.parcelFacade.deleteParcel(parcelId).subscribe((success) => {
      if (success) {
        this.router.navigate(['/app/parcels']);
      }
    });
  }
}
