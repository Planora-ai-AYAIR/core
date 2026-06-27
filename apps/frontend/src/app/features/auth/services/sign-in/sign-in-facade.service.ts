import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SignInApiService } from './sign-in-api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { LoginRequest } from '../../interfaces/sign-in/login-request';
import { ROUTES } from '../../../../shared/config/constants';
import { catchError, finalize, EMPTY, of, switchMap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class SignInFacadeService {
  private api = inject(SignInApiService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toaster = inject(ToastService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();

  login(credentials: LoginRequest): void {
    this.isLoading.set(true);

    this.api
      .login(credentials)
      .pipe(
        switchMap((response) => {
          if (response.statusCode === 200 && response.data) {
            this.authService.storeTokens(
              response.data.accessToken,
              response.data.refreshToken,
              response.data.fullName,
            );
            this.toaster.success('Welcome back! Login successful.');
            this.router.navigate([ROUTES.dashboard]);
          }
          return of(null);
        }),
        catchError((error: HttpErrorResponse) => {
          const errResponse = error.error as any;
          if (errResponse?.statusCode === 403) {
            const errorObj = errResponse.errors?.[0];
            const userId = errorObj?.metaData?.['userId'];
            if (userId) {
              this.toaster.info('Please verify your email before logging in.');
              this.router.navigate([ROUTES.confirmEmail], { state: { userId } });
            } else {
              this.toaster.error('Please verify your email before logging in.');
            }
          } else if (errResponse?.statusCode === 401) {
            this.toaster.error('Invalid email or password.');
          } else if (errResponse?.statusCode === 429) {
            this.toaster.error('Too many attempts. Try again later.');
          } else {
            this.toaster.error('An unexpected error occurred. Please try again.');
          }
          return EMPTY;
        }),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe();
  }

  logout(): void {
    const token = this.authService.accessToken;
    if (token) {
      this.api.logout(token).subscribe({
        next: () => {
          this.authService.clearTokens();
          this.toaster.success('Logged out successfully.');
          this.router.navigate([ROUTES.signIn]);
        },
        error: () => {
          this.authService.clearTokens();
          this.router.navigate([ROUTES.signIn]);
        },
      });
    } else {
      this.authService.clearTokens();
      this.router.navigate([ROUTES.signIn]);
    }
  }
}
