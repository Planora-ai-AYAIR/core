import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { LoginResponse } from '../../interfaces/sign-in/login-response';

@Injectable({ providedIn: 'root' })
export class ConfirmEmailApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  verifyOtp(userId: string, otp: string): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(
      `${this.baseUrl}${environment.Auth['verify-otp']}`,
      { userId, otp },
    );
  }

  resendOtp(userId: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}${environment.Auth['resend-otp']}`, {
      userId,
    });
  }
}
