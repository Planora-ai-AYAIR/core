import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'sign-in',
    pathMatch: 'full',
  },
  {
    path: 'sign-in',
    loadComponent: () =>
      import('./pages/sign-in-page/sign-in-page.component').then((m) => m.SignInPageComponent),
    title: 'Sign In',
    canActivate: [authGuard],
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password-page/forgot-password-page.component').then(
        (m) => m.ForgotPasswordPageComponent,
      ),
    title: 'Forget Password',
    canActivate: [authGuard],
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password-page/reset-password-page.component').then(
        (m) => m.ResetPasswordPageComponent,
      ),
    title: 'Reset Password',
    canActivate: [authGuard],
  },
  {
    path: 'sign-up',
    loadComponent: () =>
      import('./pages/sign-up-page/sign-up-page.component').then((m) => m.SignUpPageComponent),
    title: 'Sign Up',
    canActivate: [authGuard],
  },
  {
    path: 'confirm-email',
    loadComponent: () =>
      import('./pages/confirm-email-page/confirm-email-page.component').then(
        (m) => m.ConfirmEmailPageComponent,
      ),
    title: 'Confirm Email',
  },
];
