import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ResetPasswordApiService } from './reset-password-api.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { ResetPasswordRequest } from '../../interfaces/reset-password/reset-password-request';
import { ROUTES } from '../../../../shared/config/constants';
import { catchError, EMPTY, finalize, switchMap, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ResetPasswordFacadeService {
  private api = inject(ResetPasswordApiService);
  private router = inject(Router);
  private toaster = inject(ToastService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();

  resetPassword(data: ResetPasswordRequest): void {
    this.isLoading.set(true);

    this.api
      .resetPassword(data)
      .pipe(
        switchMap((response) => {
          if (response.statusCode === 200) {
            this.toaster.success('Password reset successfully. Please sign in.');
            this.router.navigate([ROUTES.signIn]);
          }
          return of(null);
        }),
        catchError((error: HttpErrorResponse) => {
          const errResponse = error.error as any;
          const status = errResponse?.statusCode || error.status; 
          const code = errResponse?.errors?.[0]?.code;

          
          if (status === 400 && code === 'INVALIDOTP') {
            this.toaster.error('Invalid or expired code. Please request a new one.');
          } else if (status === 400) {
            this.toaster.error('Please correct the highlighted errors.');
          } else if (status === 404) {
            this.toaster.error('Account not found.');
          } else if (status === 500) {
            this.toaster.error('Could not reset your password right now. Please try again.');
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
