import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { SignInApiService } from './sign-in-api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../shared/services/toaster.service';
import { AuthStateService } from '../../../../core/services/auth-state.service';

@Injectable({ providedIn: 'root' })
export class SignInFacadeService {
  private api = inject(SignInApiService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toaster = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private authState = inject(AuthStateService);

  private isLoading = signal(false);
  readonly loading = this.isLoading.asReadonly();


}
