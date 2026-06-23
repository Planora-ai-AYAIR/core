import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { STORAGE_KEYS } from '../../shared/config/constants';
import { UserSession } from '../interfaces/user-session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly REFRESH_TOKEN_KEY = STORAGE_KEYS.AUTH_TOKEN;
  private readonly USER_NAME_KEY = 'planora_user_name';

  private accessTokenSubject = new BehaviorSubject<string | null>(null);
  private currentUserSubject = new BehaviorSubject<UserSession | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    const hasRefreshToken = this.hasPersistedSession();
    const savedName = localStorage.getItem(this.USER_NAME_KEY) || undefined;

    if (hasRefreshToken) {
      this.currentUserSubject.next({
        id: '',
        email: '',
        name: savedName || 'User',
        role: 'Client',
      });
    }
  }

  // ---- Token Getters/Setters ----
  get accessToken(): string | null {
    return this.accessTokenSubject.value;
  }

  get refreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  hasPersistedSession(): boolean {
    return !!this.refreshToken;
  }

  storeTokens(accessToken: string, refreshToken: string, fullName?: string): void {
    this.accessTokenSubject.next(accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);

    if (fullName) {
      localStorage.setItem(this.USER_NAME_KEY, fullName);
    }

    const activeName = fullName || localStorage.getItem(this.USER_NAME_KEY) || undefined;
    this.decodeAndSetUser(accessToken, activeName);
  }

  setAccessToken(accessToken: string): void {
    this.accessTokenSubject.next(accessToken);
  }

  clearTokens(): void {
    this.accessTokenSubject.next(null);
    this.currentUserSubject.next(null);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_NAME_KEY);
  }

  private decodeAndSetUser(token: string, fallbackName?: string): void {
    try {
      const payloadBase64 = token.split('.')[1];
      const decoded = JSON.parse(atob(payloadBase64));

      this.currentUserSubject.next({
        id: decoded.sub,
        email: decoded.email,
        name: fallbackName || decoded.unique_name || decoded.name || decoded.email.split('@')[0],
        role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'Client',
      });
    } catch {
      this.clearTokens();
    }
  }

  // ---- Session State ----
  isAuthenticated(): boolean {
    return !!this.accessToken;
  }
}
