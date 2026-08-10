import { Routes } from '@angular/router';
import { StoreMyCart } from './my-cart/my-cart';
import { StoreLayout } from './store-layout/store-layout';
import { StoreProducts } from './products/products';
import { StoreProductDetails } from './product-details/product-details';
import { StoreHome } from './store-home/store-home';

export const storeRoutes: Routes = [
    {
        path: '',
        component: StoreLayout,
        children: [
            { path: '', component: StoreHome, pathMatch: 'full' },
            { path: 'my-cart', component: StoreMyCart },
            { path: 'products/:id', component: StoreProductDetails },
            { path: 'products', component: StoreProducts },
            { path: '**', redirectTo: '' }
        ]
    }
];
