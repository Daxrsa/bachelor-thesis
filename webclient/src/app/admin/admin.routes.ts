import { Routes } from '@angular/router';
import { AppLayout } from '../layout/component/app.layout';
import { adminGuard, authGuard } from '../auth.guard';
import { Dashboard } from '../pages/dashboard/dashboard';
import { Documentation } from '../pages/documentation/documentation';
import { Landing } from '../pages/landing/landing';
import { Notfound } from '../pages/notfound/notfound';
import { Orders } from '../pages/orders/orders';
import { PaymentsPlugin } from '../pages/payments-plugin/payments-plugin';
import { PluginCatalog } from '../pages/plugin-catalog/plugin-catalog';
import { ProductsPluginDashboard } from '../pages/products-plugin-dashboard/products-plugin-dashboard';
import { PublisherRequests } from '../pages/publisher/publisher-requests';

export const adminRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        children: [
            { path: '', component: Dashboard, canActivate: [authGuard, adminGuard] },
            { path: 'plugin-catalog', component: PluginCatalog, canActivate: [authGuard] },
            { path: 'plugin-catalog/payment-plugin', component: PaymentsPlugin, canActivate: [authGuard] },
            { path: 'plugin-catalog/products-plugin', component: ProductsPluginDashboard, canActivate: [authGuard] },
            { path: 'plugin-catalog/order-plugin', component: Orders, canActivate: [authGuard] },
            { path: 'publisher-requests', component: PublisherRequests, canActivate: [authGuard, adminGuard] },
            { path: 'uikit', loadChildren: () => import('../pages/uikit/uikit.routes') },
            { path: 'documentation', component: Documentation },
            { path: 'pages', loadChildren: () => import('../pages/pages.routes') }
        ]
    },
    { path: 'publisher', loadChildren: () => import('../pages/publisher/publisher.routes').then((m) => m.default) },
    { path: 'landing', component: Landing },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('../pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
