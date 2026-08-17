import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { RouterModule } from '@angular/router';

@Component({
    imports: [CardModule, ButtonModule, RouterModule],
    selector: 'app-dashboard',
    template: ` 
    <div class="card">
        <div class="flex justify-start">
        <p-button [routerLink]="['/store/']">Go to store<p-button/>
            <div class="flex items-center h-14">
        <img [src]="logoUrl" alt="Logo" class="h-full w-auto object-contain" />
    </div>`
})
export class Dashboard {
    logoUrl = '/demo/images/logo.webp';
}
