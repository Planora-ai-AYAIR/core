import { Routes } from '@angular/router';

export const PARCEL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/parcel-list/parcel-list.component').then((m) => m.ParcelListComponent),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./components/parcel-create/parcel-create.component').then(
        (m) => m.ParcelCreateComponent,
      ),
    title: 'New Parcel',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./components/parcel-detail/parcel-detail.component').then(
        (m) => m.ParcelDetailComponent,
      ),
    title: 'Parcel Details',
  },
];
