import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AppFloatingConfigurator } from '../../layout/component/app.floatingconfigurator';

@Component({
    selector: 'app-register-choice',
    standalone: true,
    imports: [RouterModule, ButtonModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden p-4">
            <div class="w-full max-w-4xl">
                <div class="text-center mb-10">
                    <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-2">Welcome</div>
                    <p class="text-muted-color m-0">Your account is ready. Where do you want to go?</p>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div class="bg-surface-0 dark:bg-surface-900 rounded-2xl p-8 border border-surface-200 dark:border-surface-700 flex flex-col">
                        <i class="pi pi-shop text-4xl text-primary mb-4"></i>
                        <div class="text-xl font-semibold mb-2">Main store</div>
                        <p class="text-muted-color flex-1 mb-6">Browse products, manage your cart, and check out as a customer.</p>
                        <p-button label="Go to store" icon="pi pi-arrow-right" iconPos="right" routerLink="/store" styleClass="w-full" />
                    </div>

                    <div class="bg-surface-0 dark:bg-surface-900 rounded-2xl p-8 border border-surface-200 dark:border-surface-700 flex flex-col">
                        <i class="pi pi-send text-4xl text-primary mb-4"></i>
                        <div class="text-xl font-semibold mb-2">Publisher portal</div>
                        <p class="text-muted-color flex-1 mb-6">Request publisher access and list plugins in the marketplace.</p>
                        <p-button label="Go to publisher portal" icon="pi pi-arrow-right" iconPos="right" routerLink="/publisher" styleClass="w-full" />
                    </div>
                </div>
            </div>
        </div>
    `
})
export class RegisterChoice {}
