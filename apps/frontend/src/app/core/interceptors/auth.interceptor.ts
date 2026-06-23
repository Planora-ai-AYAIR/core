import { Injectable, inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpStatusCode,
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { SignInApiService } from '../../features/auth/services/sign-in/sign-in-api.service';
import { Router } from '@angular/router';
import { PUBLIC_AUTH_ENDPOINTS, ROUTES } from '../../shared/config/constants';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private auth = inject(AuthService);
  private signInApi = inject(SignInApiService);
  private router = inject(Router);

  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.accessToken;
    if (token) {
      request = this.addToken(request, token);
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        const isPublicAuthEndpoint = PUBLIC_AUTH_ENDPOINTS.some((endpoint) =>
          request.url.includes(endpoint),
        );

        if (error.status === HttpStatusCode.Unauthorized && !isPublicAuthEndpoint) {
          return this.handle401Error(request, next);
        }
        return throwError(() => error);
      }),
    );
  }

  private addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  private handle401Error(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      const refreshToken = this.auth.refreshToken;
      if (!refreshToken) {
        this.isRefreshing = false;
        this.auth.clearTokens();
        this.router.navigate([ROUTES.signIn]);
        return throwError(() => new Error('No refresh token'));
      }

      return this.signInApi.refreshToken(refreshToken).pipe(
        switchMap((response) => {
          this.isRefreshing = false;
          const tokens = response.data!;
          this.auth.storeTokens(tokens.accessToken, tokens.refreshToken);
          this.refreshTokenSubject.next(tokens.accessToken);
          return next.handle(this.addToken(request, tokens.accessToken));
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.auth.clearTokens();
          this.router.navigate([ROUTES.signIn]);
          return throwError(() => err);
        }),
      );
    } else {
      // Wait while another request is already refreshing
      return this.refreshTokenSubject.pipe(
        filter((token) => token !== null),
        take(1),
        switchMap((token) => next.handle(this.addToken(request, token!))),
      );
    }
  }
}
