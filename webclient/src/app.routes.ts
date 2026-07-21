import { Routes } from '@angular/router';

export const appRoutes: Routes = [
    { path: 'store', loadChildren: () => import('./app/store/store.routes').then((m) => m.storeRoutes) },
    { path: '', loadChildren: () => import('./app/admin/admin.routes').then((m) => m.adminRoutes) }
];
