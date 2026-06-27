import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { ForgotPasswordRequest } from '../../interfaces/forgot-password/forgot-password-request';
import { ForgotPasswordResponse } from '../../interfaces/forgot-password/forgot-password-response';

@Injectable({ providedIn: 'root' })
export class ForgotPasswordApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  requestReset(data: ForgotPasswordRequest): Observable<ApiResponse<ForgotPasswordResponse>> {
    return this.http.post<ApiResponse<ForgotPasswordResponse>>(
      `${this.baseUrl}${environment.Auth['forgot-password']}`,
      data,
    );
  }
}
