import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { ChipModule } from 'primeng/chip';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToolbarModule } from 'primeng/toolbar';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';
import { cartItemCount, ProductService } from '@/app/pages/service/product.service';

@Component({
    selector: 'app-store-layout',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ToolbarModule,
        ButtonModule,
        AvatarModule,
        ChipModule,
        TagModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule
    ],
    providers: [ProductService],
    template: `
        <div class="w-full mx-auto p-4 md:p-6 pb-0">
            <p-toolbar styleClass="rounded-xl shadow-sm">
                <ng-template #start>
                    <div class="flex items-center gap-3">
                        <div class="flex items-center h-14">
                            <img [src]="logoUrl" alt="Logo" class="h-full w-auto object-contain" />
                        </div>
                    </div>
                </ng-template>

                <ng-template #center>
                    <p-iconfield class="w-[24rem] md:w-[30rem] max-w-full">
                        <p-inputicon class="pi pi-search" />
                        <input pInputText type="text" class="w-full" placeholder="Search products..." [(ngModel)]="searchTerm" />
                    </p-iconfield>
                </ng-template>

                <ng-template #end>
                    <div class="flex items-center gap-2">
                        <span *ngIf="currentUserEmail" class="hidden md:inline text-sm text-surface-600 dark:text-surface-300">{{ currentUserEmail }}</span>
                        <p-button *ngIf="!currentUserEmail" label="Login" icon="pi pi-sign-in" severity="secondary" [outlined]="true" [routerLink]="['/auth/login']"></p-button>
                        <p-button label="Products" icon="pi pi-th-large" severity="secondary" [outlined]="true" [routerLink]="['/store/products']"></p-button>
                        <div class="relative">
                            <p-button icon="pi pi-shopping-cart" [outlined]="true" [routerLink]="['/store/my-cart']"></p-button>
                            <span
                                *ngIf="cartItemCount() > 0"
                                class="absolute -top-2 -left-2 min-w-5 h-5 px-1 rounded-full bg-primary text-primary-contrast text-xs font-semibold flex items-center justify-center pointer-events-none"
                            >
                                {{ cartItemCount() }}
                            </span>
                        </div>
                        <p-button *ngIf="currentUserEmail" icon="pi pi-sign-out" severity="secondary" [outlined]="true" (onClick)="logout()"></p-button>
                    </div>
                </ng-template>
            </p-toolbar>
        </div>

        <router-outlet></router-outlet>
    `
})
export class StoreLayout implements OnInit {
    private readonly apiBase = 'http://localhost:8080';

    logoUrl = '/demo/images/logo.webp';

    searchTerm = '';
    currentUserEmail: string | null = null;
    readonly cartItemCount = cartItemCount;

    constructor(
        private http: HttpClient,
        private cdr: ChangeDetectorRef,
        private router: Router,
        private productService: ProductService
    ) { }

    ngOnInit() {
        void this.loadCurrentUser();
    }

    async loadCurrentUser() {
        const token = localStorage.getItem(TOKEN_STORAGE_KEY);
        if (!token) {
            this.currentUserEmail = null;
            cartItemCount.set(0);
            this.cdr.detectChanges();
            return;
        }

        try {
            const me = await firstValueFrom(this.http.get<{ email?: string }>(`${this.apiBase}/api/auth/me`));
            this.currentUserEmail = me?.email ?? null;
        } catch {
            this.currentUserEmail = null;
        } finally {
            this.cdr.detectChanges();
        }

        await this.productService.refreshCartItemCount();
    }

    logout() {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        this.currentUserEmail = null;
        cartItemCount.set(0);
        this.cdr.detectChanges();
        void this.router.navigate(['/store']);
    }
}
