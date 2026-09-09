import { Routes } from '@angular/router';
import { authGuard, publisherGuard } from '../../auth.guard';
import { PublisherDashboard } from './publisher-dashboard';
import { PublisherLayout } from './publisher-layout';
import { PublisherPluginForm } from './publisher-plugin-form';
import { PublisherPlugins } from './publisher-plugins';

export default [
    {
        path: '',
        component: PublisherLayout,
        canActivate: [authGuard],
        children: [
            { path: '', component: PublisherDashboard },
            { path: 'plugins', component: PublisherPlugins, canActivate: [publisherGuard] },
            { path: 'plugins/new', component: PublisherPluginForm, canActivate: [publisherGuard] },
            { path: 'plugins/:pluginId', component: PublisherPluginForm, canActivate: [publisherGuard] }
        ]
    }
] as Routes;
