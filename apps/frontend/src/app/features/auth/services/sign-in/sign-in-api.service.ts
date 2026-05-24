import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { LoginRequest } from '../../interfaces/sign-in/login-request';
import { LoginResponse } from '../../interfaces/sign-in/login-response';

@Injectable({ providedIn: 'root' })
export class SignInApiService {
  private http = inject(HttpClient);

}