import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CurrentUserService } from '@/app/auth/current-user.service';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { MarketplaceApi, MarketplaceListingDto, PublisherRequestDto } from './marketplace.api';

@Component({
    selector: 'app-publisher-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, TextareaModule],
    template: `
        <div class="card">
            <div class="flex items-center justify-between mb-4">
                <div>
                    <div class="font-semibold text-xl">Publisher Portal</div>
                    <div class="text-sm text-muted-color">
                        Signed in as {{ currentUser.user()?.email || '…' }} · role {{ currentUser.role() }}
                    </div>
                </div>
                <div class="flex gap-2" *ngIf="currentUser.isPublisher()">
                    <p-button label="My plugins" icon="pi pi-box" routerLink="/publisher/plugins" />
                    <p-button label="Publish plugin" icon="pi pi-plus" severity="success" routerLink="/publisher/plugins/new" />
                </div>
            </div>

            <div *ngIf="error" class="text-red-500 mb-4"><b>Error:</b> {{ error }}</div>
            <div *ngIf="success" class="text-green-600 mb-4">{{ success }}</div>

            <ng-container *ngIf="!currentUser.isPublisher(); else publisherHome">
                <div class="p-4 border rounded-lg surface-border mb-4">
                    <div class="font-semibold text-lg mb-2">Become a publisher</div>
                    <p class="text-sm text-muted-color mb-3">
                        Publishers can list Docker images in the marketplace. An admin must approve your request.
                        Log in again after approval so your token includes the new role.
                    </p>

                    <div *ngIf="request?.status === 'pending'" class="text-sm mb-3">
                        Request <b>pending</b> since {{ request?.createdAt | date: 'medium' }}.
                    </div>
                    <div *ngIf="request?.status === 'rejected'" class="text-sm mb-3">
                        Previous request was <b>rejected</b>. You can submit a new one.
                    </div>
                    <div *ngIf="request?.status === 'approved'" class="text-sm mb-3">
                        Approved. Sign out and sign in again, then return here.
                    </div>

                    <label class="block font-medium mb-2">Message (optional)</label>
                    <textarea pTextarea [(ngModel)]="requestMessage" rows="3" class="w-full mb-3" placeholder="What plugin are you planning to publish?"></textarea>
                    <p-button
                        label="Request publisher access"
                        icon="pi pi-send"
                        [loading]="requesting"
                        [disabled]="request?.status === 'pending'"
                        (onClick)="submitRequest()"
                    />
                </div>
            </ng-container>

            <ng-template #publisherHome>
                <div class="grid grid-cols-1 md:grid-cols-3 gap-3 mb-6">
                    <div class="p-4 border rounded-lg surface-border">
                        <div class="text-xs text-muted-color mb-1">Your listings</div>
                        <div class="text-2xl font-semibold">{{ listings.length }}</div>
                    </div>
                    <div class="p-4 border rounded-lg surface-border">
                        <div class="text-xs text-muted-color mb-1">Installed on this instance</div>
                        <div class="text-2xl font-semibold">{{ installedCount }}</div>
                    </div>
                    <div class="p-4 border rounded-lg surface-border">
                        <div class="text-xs text-muted-color mb-1">Last published</div>
                        <div class="font-medium">{{ lastPublished || '—' }}</div>
                    </div>
                </div>
            </ng-template>

            <div class="p-4 border rounded-lg surface-border">
                <div class="font-semibold text-lg mb-2">How publishing works</div>
                <ol class="list-decimal pl-5 text-sm leading-7">
                    <li>Scaffold with <code>dotnet new ecommerce-plugin</code> and reference the contract NuGet packages.</li>
                    <li>Build and <code>docker push</code> the image to a registry this host can pull.</li>
                    <li>Publish the <code>plugin.json</code> manifest from <a routerLink="/publisher/plugins/new">Publish plugin</a>.</li>
                    <li>Operators install it from the Plugin Catalog. Traffic always goes through <code>/api/p/&#123;pluginId&#125;/…</code>.</li>
                </ol>
                <p class="text-sm text-muted-color mt-3">
                    Unpublishing a listing does not uninstall running containers.
                </p>
            </div>
        </div>
    `
})
export class PublisherDashboard implements OnInit {
    private readonly api = inject(MarketplaceApi);
    private readonly cdr = inject(ChangeDetectorRef);
    readonly currentUser = inject(CurrentUserService);

    listings: MarketplaceListingDto[] = [];
    request: PublisherRequestDto | null = null;
    requestMessage = '';
    requesting = false;
    error = '';
    success = '';

    get installedCount(): number {
        return this.listings.filter((l) => l.installed).length;
    }

    get lastPublished(): string | null {
        if (this.listings.length === 0) return null;
        const latest = [...this.listings].sort(
            (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
        )[0];
        return new Date(latest.updatedAt).toLocaleString();
    }

    async ngOnInit() {
        await this.currentUser.refresh();
        this.error = '';
        try {
            if (this.currentUser.isPublisher()) {
                this.listings = await this.api.listMine();
            } else {
                this.request = await this.api.myPublisherRequest();
            }
        } catch (e) {
            this.error = this.api.errorMessage(e);
        } finally {
            this.cdr.detectChanges();
        }
    }

    async submitRequest() {
        this.requesting = true;
        this.error = '';
        this.success = '';
        try {
            this.request = await this.api.requestPublisher(this.requestMessage);
            this.success = 'Request submitted. An admin will review it.';
        } catch (e) {
            this.error = this.api.errorMessage(e);
        } finally {
            this.requesting = false;
            this.cdr.detectChanges();
        }
    }
}
