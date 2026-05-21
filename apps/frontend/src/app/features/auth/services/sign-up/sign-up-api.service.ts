import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SignUpResponse } from '../../interfaces/sign-up/sign-up-response';
import { ApiResponse } from '../../../../core/interfaces/api-response';
import { environment } from '../../../../../environments/environment';
import { SignUpRequest } from '../../interfaces/sign-up/sign-up-request';

@Injectable({ providedIn: 'root' })
export class SignUpApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;



}
