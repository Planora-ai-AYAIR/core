import { Routes } from '@angular/router';
import { PublicLayoutComponent } from './shared/components/layouts/public-layout/public-layout.component';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routing').then((m) => m.AUTH_ROUTES),
  },

  {
    path: '',
    component: PublicLayoutComponent,
    children: [
      {
        path: 'home',
        loadComponent: () =>
          import('./features/landingpage/pages/landingpage/landingpage.component').then(
            (m) => m.LandingpageComponent,
          ),
      },

      {
        path: '',
        redirectTo: 'home',
        pathMatch: 'full',
      },
      // Privacy Policy
    ],
  },

  // 404 Error
  {
    path: 'Error404',
    loadComponent: () => import('./shared/components/not-found/not-found').then((m) => m.NotFound),
  },

  // Wildcard - redirect to 404
  {
    path: '**',
    redirectTo: 'Error404',
    pathMatch: 'full',
  },
];
