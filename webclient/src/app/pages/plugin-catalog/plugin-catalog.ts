import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom, Observable, timeout } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CurrentUserService } from '@/app/auth/current-user.service';

interface MarketplaceEntry {
    manifest: {
        id: string;
        name: string;
        version: string;
        description: string;
        publisher: string;
        image?: string;
        containerPort?: number;
        healthEndpoint?: string;
        hostApi?: string;
        permissions?: string[];
        uiExtensions?: Record<string, string>;
    };
    installed: boolean;
}

@Component({
    selector: 'app-plugin-catalog',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule],
    template: `
        <div class="card">
            <div class="flex items-center justify-between mb-4">
                <div class="font-semibold text-xl">Plugin Catalog</div>
                <p-button label="Refresh" icon="pi pi-refresh" [loading]="loading" (onClick)="load()" severity="secondary" />
            </div>

            <div *ngIf="error" class="text-red-500 mb-4"><b>Error:</b> {{ error }}</div>

            <div *ngIf="currentUser.isPublisher()" class="mb-6 p-4 border rounded-lg surface-border flex items-center justify-between gap-3">
                <div>
                    <div class="font-medium">Publish plugins from the publisher portal</div>
                    <div class="text-sm text-muted-color">Listings appear here after you publish a manifest. Images stay in the container registry.</div>
                </div>
                <p-button label="Open portal" icon="pi pi-send" routerLink="/publisher" />
            </div>

            <div class="mb-6">
                <div class="font-semibold text-lg mb-3">Installed ({{ installed.length }})</div>
                <div *ngIf="!loading && installed.length === 0" class="text-muted-color">No plugins installed.</div>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div *ngFor="let entry of installed" class="p-4 border rounded-lg surface-border flex flex-col gap-3">
                        <div>
                            <button
                                type="button"
                                class="font-semibold text-left hover:underline cursor-pointer"
                                [disabled]="!dashboardRoute(entry.manifest.id)"
                                (click)="openPlugin(entry.manifest.id)"
                            >
                                {{ entry.manifest.name }}
                            </button>
                            <div class="text-sm text-muted-color mb-2">v{{ entry.manifest.version }} · {{ entry.manifest.publisher }}</div>
                            <p class="text-sm">{{ entry.manifest.description }}</p>
                        </div>

                        <div *ngIf="greetings[entry.manifest.id]" class="text-sm">
                            <b>Greeting:</b> {{ greetings[entry.manifest.id] }}
                        </div>

                        <div class="flex flex-wrap gap-2 mt-auto">
                            <p-button
                                *ngIf="greetingPath(entry.manifest.id)"
                                label="Call Greeting"
                                icon="pi pi-comment"
                                severity="contrast"
                                [loading]="busyPluginId === entry.manifest.id"
                                (onClick)="callGreeting(entry.manifest.id)"
                            />
                            <p-button
                                label="Uninstall"
                                icon="pi pi-trash"
                                severity="danger"
                                [loading]="busyPluginId === entry.manifest.id"
                                (onClick)="uninstall(entry.manifest.id)"
                            />
                        </div>
                    </div>
                </div>
            </div>

            <div>
                <div class="font-semibold text-lg mb-3">Not Installed ({{ notInstalled.length }})</div>
                <div *ngIf="!loading && notInstalled.length === 0" class="text-muted-color">No available plugins.</div>
                <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div *ngFor="let entry of notInstalled" class="p-4 border rounded-lg surface-border flex flex-col gap-3">
                        <div>
                            <button
                                type="button"
                                class="font-semibold text-left hover:underline cursor-pointer"
                                [disabled]="!dashboardRoute(entry.manifest.id)"
                                (click)="openPlugin(entry.manifest.id)"
                            >
                                {{ entry.manifest.name }}
                            </button>
                            <div class="text-sm text-muted-color mb-2">v{{ entry.manifest.version }} · {{ entry.manifest.publisher }}</div>
                            <p class="text-sm">{{ entry.manifest.description }}</p>
                        </div>

                        <div class="flex flex-wrap gap-2 mt-auto">
                            <p-button
                                label="Install"
                                icon="pi pi-download"
                                severity="success"
                                [loading]="busyPluginId === entry.manifest.id"
                                (onClick)="install(entry.manifest.id)"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class PluginCatalog implements OnInit {
    private readonly http = inject(HttpClient);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly router = inject(Router);
    readonly currentUser = inject(CurrentUserService);
    private readonly apiBase = 'http://localhost:8080';
    private readonly requestTimeoutMs = 45000;

    entries: MarketplaceEntry[] = [];
    loading = false;
    error = '';
    busyPluginId = '';
    greetings: Record<string, string> = {};

    get installed(): MarketplaceEntry[] {
        return this.entries.filter((e) => e.installed);
    }

    get notInstalled(): MarketplaceEntry[] {
        return this.entries.filter((e) => !e.installed);
    }

    ngOnInit() {
        void this.currentUser.refresh();
        void this.load();
    }

    async load() {
        this.loading = true;
        this.error = '';
        try {
            this.entries = await this.send(
                this.http.get<MarketplaceEntry[]>(`${this.apiBase}/api/plugins/marketplace`)
            );
        } catch (e: any) {
            this.error = this.errorMessage(e);
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    async install(pluginId: string) {
        await this.run(pluginId, async () => {
            await this.send(
                this.http.post(
                    `${this.apiBase}/api/plugins/${pluginId}/install`,
                    { grantedPermissions: [] }
                )
            );
            await this.load();
        });
    }

    async uninstall(pluginId: string) {
        await this.run(pluginId, async () => {
            await this.send(
                this.http.delete(`${this.apiBase}/api/plugins/${pluginId}`)
            );
            delete this.greetings[pluginId];
            await this.load();
        });
    }

    async callGreeting(pluginId: string) {
        const path = this.greetingPath(pluginId);
        if (!path) return;

        await this.run(pluginId, async () => {
            const res = await this.send(
                this.http.get<{ message: string }>(`${this.apiBase}/api/p/${pluginId}/${path}`)
            );
            this.greetings[pluginId] = res.message;
        });
    }

    greetingPath(pluginId: string): string | null {
        return {
            'hello-plugin': 'greeting',
            'products-plugin': 'products/greeting',
            'cart-plugin': 'cart/greeting',
            'payment-plugin': 'payment/greeting',
            'files-plugin': 'files/greeting',
            'order-plugin': 'orders/greeting'
        }[pluginId] ?? null;
    }

    dashboardRoute(pluginId: string): string | null {
        return {
            'products-plugin': '/plugin-catalog/products-plugin',
            'payment-plugin': '/plugin-catalog/payment-plugin',
            'order-plugin': '/plugin-catalog/order-plugin'
        }[pluginId] ?? null;
    }

    openPlugin(pluginId: string) {
        const route = this.dashboardRoute(pluginId);
        if (!route) return;

        void this.router.navigateByUrl(route);
    }

    private async run(pluginId: string, work: () => Promise<void>) {
        this.busyPluginId = pluginId;
        this.error = '';
        try {
            await work();
        } catch (e: any) {
            this.error = this.errorMessage(e);
        } finally {
            this.busyPluginId = '';
            this.cdr.detectChanges();
        }
    }

    private send<T>(obs: Observable<T>) {
        return firstValueFrom(obs.pipe(timeout(this.requestTimeoutMs)));
    }

    private errorMessage(e: any): string {
        return e?.error?.error ?? e?.message ?? 'Request failed';
    }
}
