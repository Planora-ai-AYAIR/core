import { Injectable, signal, inject } from '@angular/core';
import { ParcelApiService } from './parcel-api.service';
import { Observable, catchError, of, tap } from 'rxjs';
import { ToastService } from '../../../shared/services/toaster.service';
import { CreateParcelRequest } from '../interfaces/parcel-new/create-parcel-request';
import { CreateParcelResponse } from '../interfaces/parcel-new/create-parcel-response';

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
}
