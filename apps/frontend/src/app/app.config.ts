import {
  APP_INITIALIZER,
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
// 1. Add provideHttpClient and withInterceptorsFromDi imports
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { routes } from './app.routes';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { SignInApiService } from './features/auth/services/sign-in/sign-in-api.service';
import { AuthService } from './core/services/auth.service';
import { catchError, of, tap } from 'rxjs';

function initializeAuth(auth: AuthService, signInApi: SignInApiService): () => Promise<void> {
  return () => {
    const refreshToken = auth.refreshToken;
    if (!refreshToken) {
      return Promise.resolve();
    }

    return new Promise<void>((resolve) => {
      signInApi
        .refreshToken(refreshToken)
        .pipe(
          tap((response) => {
            if (response.statusCode === 200 && response.data) {
              auth.storeTokens(response.data.accessToken, response.data.refreshToken);
            } else {
              auth.clearTokens();
            }
          }),
          catchError(() => {
            auth.clearTokens();
            return of(null);
          }),
        )
        .subscribe(() => resolve());
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService, SignInApiService],
      multi: true,
    },
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'top',
      }),
    ),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
  ],
};
