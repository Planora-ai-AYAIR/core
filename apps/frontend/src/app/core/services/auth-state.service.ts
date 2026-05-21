import { Injectable, signal } from '@angular/core';
import { UserInfo } from '../interfaces/user-info';

const STORAGE_KEY = 'user_info';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  readonly user = signal<UserInfo | null>(null);
  readonly fullName = signal<string | null>(null);
  readonly email = signal<string | null>(null);
  readonly role = signal<'patient' | 'doctor' | null>(null);

  constructor() {
    // Restore from sessionStorage on app start
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        const data: UserInfo = JSON.parse(stored);
        this.setUser(data);
      } catch (e) {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    }
  }

  setUser(data: UserInfo): void {
    this.user.set(data);
    this.fullName.set(data.fullName);
    this.email.set(data.email);
    this.role.set(data.role);

    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }

  clear(): void {
    this.user.set(null);
    this.fullName.set(null);
    this.email.set(null);
    this.role.set(null);
    sessionStorage.removeItem(STORAGE_KEY);
  }
}
