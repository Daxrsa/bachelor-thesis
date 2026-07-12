import { Component } from '@angular/core';
import { HelloPluginWidget } from './hello-plugin-widget';

@Component({
    selector: 'app-plugin-catalog',
    standalone: true,
    imports: [HelloPluginWidget],
    template: ` <div class="card">
        <div class="font-semibold text-xl mb-4">Plugin Catalog</div>
        <p>hello plugin catalog</p>
    </div>

    <app-hello-plugin-widget />`
})
export class PluginCatalog {}
