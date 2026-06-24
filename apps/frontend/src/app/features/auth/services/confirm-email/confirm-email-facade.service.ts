import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmEmailApiService } from './confirm-email-api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { ROUTES } from '../../../../shared/config/constants';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, EMPTY, finalize, map, Observable, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ConfirmEmailFacadeService {
  private api = inject(ConfirmEmailApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toaster = inject(ToastService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();

  verifyOtp(userId: string, otp: string): void {
    this.isLoading.set(true);
    this.api
      .verifyOtp(userId, otp)
      .pipe(
        catchError((err: HttpErrorResponse) => {
          const code = err.error?.errors?.[0]?.code;

          if (code === 'INVALIDOTP') {
            this.toaster.error('Invalid or expired OTP.');
          } else if (code === 'USER_NOT_FOUND') {
            this.toaster.error('Account not found. Please sign up again.');
            this.router.navigate([ROUTES.signUp]);
          } else {
            this.toaster.error('Verification failed. Please try again.');
          }
          return EMPTY;
        }),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe((response) => {
        if (response?.statusCode === 200 && response.data) {
          this.auth.storeTokens(response.data.accessToken, response.data.refreshToken);
          this.toaster.success('Email confirmed! Welcome aboard.');
          this.router.navigate([ROUTES.dashboard]);
        }
      });
  }

  resendOtp(userId: string): Observable<void> {
    return this.api.resendOtp(userId).pipe(
      tap(() => this.toaster.info('A new OTP has been sent.')),
      map(() => undefined),
      catchError((err: HttpErrorResponse) => {
        const code = err.error?.errors?.[0]?.code;
        if (code === 'EMAIL_ALREADY_VERIFIED') {
          this.toaster.info('Your email is already verified. Please sign in.');
          this.router.navigate([ROUTES.signIn]);
        } else if (code === 'USER_NOT_FOUND') {
          this.toaster.error('Account not found. Please sign up again.');
          this.router.navigate([ROUTES.signUp]);
        } else {
          this.toaster.error('Could not resend OTP. Please try again.');
        }
        return EMPTY;
      }),
    );
  }
}
