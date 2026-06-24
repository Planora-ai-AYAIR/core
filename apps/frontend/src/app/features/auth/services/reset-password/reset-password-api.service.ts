import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { ResetPasswordRequest } from '../../interfaces/reset-password/reset-password-request';
import { ResetPasswordResponse } from '../../interfaces/reset-password/reset-password-response';

@Injectable({ providedIn: 'root' })
export class ResetPasswordApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  resetPassword(data: ResetPasswordRequest): Observable<ApiResponse<ResetPasswordResponse>> {
    return this.http.post<ApiResponse<ResetPasswordResponse>>(
      `${this.baseUrl}${environment.Auth['reset-password']}`,
      data,
    );
  }
}
