import { Injectable, inject, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { SignUpApiService } from './sign-up-api.service';
import { ToastService } from '../../../../shared/services/toaster.service';

@Injectable({ providedIn: 'root' })
export class SignUpFacadeService {
  private api = inject(SignUpApiService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private toaster = inject(ToastService);


}
