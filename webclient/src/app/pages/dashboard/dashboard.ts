import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { RouterModule } from '@angular/router';

@Component({
    imports: [CardModule, ButtonModule, RouterModule],
    selector: 'app-dashboard',
    template: ` 
    <div class="card">
    </div>`
})
export class Dashboard {
    logoUrl = '/demo/images/logo.webp';
}
