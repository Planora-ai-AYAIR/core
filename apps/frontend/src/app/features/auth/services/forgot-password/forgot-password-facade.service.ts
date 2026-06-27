import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ForgotPasswordApiService } from './forgot-password-api.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { ForgotPasswordRequest } from '../../interfaces/forgot-password/forgot-password-request';
import { ROUTES } from '../../../../shared/config/constants';
import { catchError, EMPTY, finalize, switchMap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ForgotPasswordFacadeService {
  private api = inject(ForgotPasswordApiService);
  private router = inject(Router);
  private toaster = inject(ToastService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();

  requestReset(data: ForgotPasswordRequest): void {
    this.isLoading.set(true);

    this.api
      .requestReset(data)
      .pipe(
        switchMap((response) => {
          if (response.statusCode === 200 && response.data) {
            this.toaster.info('A reset code has been sent.');
            this.router.navigate([ROUTES.resetPassword], {
              state: { userId: response.data.userId },
            });
          }
          return EMPTY;
        }),
        catchError((error: HttpErrorResponse) => {
          const errResponse = error.error as any;
          if (errResponse?.statusCode === 404) {
            this.toaster.error('No account found with those details.');
          } else {
            this.toaster.error('An unexpected error occurred. Please try again.');
          }
          return EMPTY;
        }),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe();
  }
}
