import { Routes } from '@angular/router';
import { StoreCheckout } from './checkout/checkout';
import { StoreMyCart } from './my-cart/my-cart';
import { StoreMyPayments } from './my-payments/my-payments';
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
            { path: 'my-payments', component: StoreMyPayments },
            { path: 'checkout', component: StoreCheckout },
            { path: 'products/:id', component: StoreProductDetails },
            { path: 'products', component: StoreProducts },
            { path: '**', redirectTo: '' }
        ]
    }
];
