import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private accessToken: string | null = null;
  private _refreshToken: string | null = null;

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

}
