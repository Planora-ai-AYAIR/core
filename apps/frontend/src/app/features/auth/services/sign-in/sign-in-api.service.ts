// sign-in-api.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { LoginRequest } from '../../interfaces/sign-in/login-request'; // adjust path
import { LoginResponse } from '../../interfaces/sign-in/login-response';
import { AuthTokensResponse } from '../../../../core/interfaces/auth-tokens-response';

@Injectable({ providedIn: 'root' })
export class SignInApiService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}`;

  login(credentials: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(
      `${this.baseUrl}${environment.Auth.login}`,
      credentials,
    );
  }

  refreshToken(refreshToken: string): Observable<ApiResponse<AuthTokensResponse>> {
    return this.http.post<ApiResponse<AuthTokensResponse>>(
      `${this.baseUrl}${environment.Auth['refresh-token']}`,
      {
        refreshToken,
      },
    );
  }

  logout(accessToken: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}${environment.Auth.logout}`, null, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
  }
}
