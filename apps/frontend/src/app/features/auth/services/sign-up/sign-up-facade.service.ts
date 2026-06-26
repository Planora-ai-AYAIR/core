import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SignUpApiService } from './sign-up-api.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { SignUpRequest } from '../../interfaces/sign-up/sign-up-request';
import { ROUTES } from '../../../../shared/config/constants';
import { catchError, finalize, EMPTY, switchMap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class SignUpFacadeService {
  private api = inject(SignUpApiService);
  private router = inject(Router);
  private toaster = inject(ToastService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();

  register(data: SignUpRequest): void {
    this.isLoading.set(true);

    this.api
      .register(data)
      .pipe(
        switchMap((response) => {
          if (response.statusCode === 201 && response.data) {
            this.toaster.info('Account created. Please verify your email.');
            this.router.navigate([ROUTES.confirmEmail], {
              state: { userId: response.data.id },
            });
          }
          return EMPTY;
        }),
        catchError((error: HttpErrorResponse) => {
          const errResponse = error.error as any;
          const status = errResponse?.statusCode;

          if (status === 409) {
            const code = errResponse.errors?.[0]?.code;
            if (code === 'EMAILALREADYEXISTS') {
              this.toaster.error('An account with this email already exists.');
            } else if (code === 'PHONE_ALREADY_EXISTS') {
              this.toaster.error('This phone number is already in use.');
            } else {
              this.toaster.error('Conflict occurred. Please check your details.');
            }
          } else if (status === 400) {
            // Validation errors – we could extract field-level messages, but for now show generic
            this.toaster.error('Please correct the highlighted errors.');
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
