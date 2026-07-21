import { Routes } from '@angular/router';
import { StoreProducts } from './products/products';
import { StoreHome } from './store-home/store-home';

export const storeRoutes: Routes = [
    { path: '', component: StoreHome, pathMatch: 'full' },
    { path: 'products', component: StoreProducts },
    { path: '**', redirectTo: '' }
];
