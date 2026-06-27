import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { SignUpRequest } from '../../interfaces/sign-up/sign-up-request';
import { SignUpResponse } from '../../interfaces/sign-up/sign-up-response';

@Injectable({ providedIn: 'root' })
export class SignUpApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  register(data: SignUpRequest): Observable<ApiResponse<SignUpResponse>> {
    return this.http.post<ApiResponse<SignUpResponse>>(
      `${this.baseUrl}${environment.Auth.register}`,
      data,
    );
  }
}
