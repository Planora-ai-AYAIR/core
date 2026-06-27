import { Injectable, signal, inject } from '@angular/core';
import { ParcelApiService } from './parcel-api.service';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { ToastService } from '../../../shared/services/toaster.service';
import { CreateParcelRequest } from '../interfaces/parcel-new/create-parcel-request';
import { CreateParcelResponse } from '../interfaces/parcel-new/create-parcel-response';
import { ParcelDetailResponse } from '../interfaces/parcel-detail/parcel-detail-response';
import { ParcelListResponse } from '../interfaces/parcel-list/parcel-list-response';

@Injectable({ providedIn: 'root' })
export class ParcelFacadeService {
  private api = inject(ParcelApiService);
  private toast = inject(ToastService);

  readonly creating = signal(false);
  readonly error = signal<string | null>(null);

  createParcel(request: CreateParcelRequest): Observable<CreateParcelResponse | null> {
    this.creating.set(true);
    this.error.set(null);

    return this.api.createParcel(request).pipe(
      tap((response) => {
        this.creating.set(false);
        this.toast.success('Parcel created successfully');
      }),
      catchError((err) => {
        this.creating.set(false);

        // Extract the envelope
        const envelope = err?.error;
        const message = envelope?.message ?? 'Failed to create parcel';
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

  getMyParcels(): Observable<ParcelListResponse[] | null> {
    return this.api.getMyParcels().pipe(
      catchError((err) => {
        const envelope = err?.error;
        const message = envelope?.message ?? 'Failed to load parcels';
        const errors: any[] = envelope?.errors ?? [];
        if (errors.length > 0) {
          errors.forEach((e) => this.toast.error(e.message));
        } else {
          this.toast.error(message);
        }
        this.error.set(message);
        return of(null);
      }),
    );
  }

  getParcelById(parcelId: string): Observable<ParcelDetailResponse | null> {
    return this.api.getParcelById(parcelId).pipe(
      catchError((err) => {
        const envelope = err?.error;
        const message = envelope?.message ?? 'Failed to load parcel details';
        const errors: any[] = envelope?.errors ?? [];
        if (errors.length > 0) {
          errors.forEach((e) => this.toast.error(e.message));
        } else {
          this.toast.error(message);
        }
        this.error.set(message);
        return of(null);
      }),
    );
  }

  deleteParcel(parcelId: string): Observable<boolean> {
    return this.api.deleteParcel(parcelId).pipe(
      tap(() => {
        this.toast.success('Parcel deleted successfully');
      }),
      map(() => true),
      catchError((err) => {
        const envelope = err?.error;
        const message = envelope?.message ?? 'Failed to delete parcel';
        this.toast.error(message);
        this.error.set(message);
        return of(false);
      }),
    );
  }
}
