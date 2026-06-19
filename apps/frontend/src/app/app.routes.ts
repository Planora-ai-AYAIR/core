import { Routes } from '@angular/router';
import { PublicLayoutComponent } from './shared/components/layouts/public-layout/public-layout.component';
import { AuthenticatedLayoutComponent } from './shared/components/layouts/authenticated-layout/authenticated-layout.component';

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

  {
    path: 'app',
    component: AuthenticatedLayoutComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard-home/dashboard-home.component').then(
            (m) => m.DashboardHomeComponent,
          ),
        title: 'Dashboard',
      },
      {
        path: 'parcels',
        loadChildren: () =>
          import('./features/parcels/parcels.routing').then((m) => m.PARCEL_ROUTES),
        title: 'Parcels',
      },
      {
        path: 'analyses',
        loadChildren: () =>
          import('./features/analyses/analyses.routing').then((m) => m.ANALYSIS_ROUTES),
        title: 'Analyses',
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },

  // 404 Error
  {
    path: 'not-found',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },

  // Wildcard - redirect to 404
  {
    path: '**',
    redirectTo: 'not-found',
    pathMatch: 'full',
  },
];
