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
    // Dynamically apply token if it exists
    request = this.injectToken(request);

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

  // Helper method to guarantee it grabs whatever is current in state
  private injectToken(request: HttpRequest<unknown>, customToken?: string): HttpRequest<unknown> {
    const token = customToken || this.auth.accessToken;
    if (token) {
      return request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });
    }
    return request;
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

          // 1. Persist the new tokens to local storage and BehaviorSubject
          this.auth.storeTokens(tokens.accessToken, tokens.refreshToken);
          this.refreshTokenSubject.next(tokens.accessToken);

          // 2. Clear old headers entirely and build a fresh request copy with the new token
          const retriedRequest = request.clone({
            setHeaders: {
              Authorization: `Bearer ${tokens.accessToken}`,
            },
          });

          return next.handle(retriedRequest);
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.auth.clearTokens();
          this.router.navigate([ROUTES.signIn]);
          return throwError(() => err);
        }),
      );
    } else {
      return this.refreshTokenSubject.pipe(
        filter((token) => token !== null),
        take(1),
        switchMap((token) => {
          const retriedRequest = request.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
            },
          });
          return next.handle(retriedRequest);
        }),
      );
    }
  }
}
