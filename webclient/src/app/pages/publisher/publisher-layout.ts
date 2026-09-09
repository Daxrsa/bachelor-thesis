import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { CurrentUserService } from '@/app/auth/current-user.service';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';

@Component({
    selector: 'app-publisher-layout',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule, ToolbarModule],
    template: `
        <div class="min-h-screen bg-surface-50 dark:bg-surface-950">
            <div class="mx-auto max-w-[96rem] p-4 md:p-6 pb-0">
                <p-toolbar styleClass="rounded-xl shadow-sm">
                    <ng-template #start>
                        <div class="font-semibold text-lg">Publisher portal</div>
                    </ng-template>
                    <ng-template #center>
                        <div class="flex items-center gap-2">
                            <p-button
                                label="Portal"
                                icon="pi pi-send"
                                [outlined]="!isActive('/publisher', true)"
                                routerLink="/publisher"
                            />
                            <p-button
                                label="My plugins"
                                icon="pi pi-box"
                                [outlined]="!isActive('/publisher/plugins')"
                                routerLink="/publisher/plugins"
                            />
                        </div>
                    </ng-template>
                    <ng-template #end>
                        <div class="flex items-center gap-2">
                            <span class="hidden md:inline text-sm text-surface-600 dark:text-surface-300">{{ currentUser.user()?.email }}</span>
                            <p-button label="Store" icon="pi pi-shop" severity="secondary" [outlined]="true" routerLink="/store" />
                            <p-button icon="pi pi-sign-out" severity="secondary" [outlined]="true" (onClick)="logout()" />
                        </div>
                    </ng-template>
                </p-toolbar>
            </div>

            <div class="mx-auto max-w-[96rem] p-4 md:p-6">
                <router-outlet></router-outlet>
            </div>
        </div>
    `
})
export class PublisherLayout implements OnInit {
    readonly currentUser = inject(CurrentUserService);
    private readonly router = inject(Router);
    private readonly cdr = inject(ChangeDetectorRef);

    async ngOnInit() {
        await this.currentUser.refresh();
        this.cdr.detectChanges();
    }

    isActive(path: string, exact = false): boolean {
        const url = this.router.url.split('?')[0];
        return exact ? url === path : url === path || url.startsWith(`${path}/`);
    }

    logout() {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        this.currentUser.user.set(null);
        void this.router.navigate(['/auth/login']);
    }
}
