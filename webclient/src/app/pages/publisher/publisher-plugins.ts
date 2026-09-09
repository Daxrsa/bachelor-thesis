import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { MarketplaceApi, MarketplaceListingDto } from './marketplace.api';

@Component({
    selector: 'app-publisher-plugins',
    standalone: true,
    imports: [CommonModule, RouterModule, TableModule, ButtonModule, TagModule, ToastModule, ToolbarModule, ConfirmDialogModule],
    providers: [MessageService, ConfirmationService],
    template: `
        <p-toast />
        <p-confirmdialog />
        <div class="card">
            <div class="flex items-center justify-between mb-4">
                <div>
                    <div class="font-semibold text-xl">My plugins</div>
                    <div class="text-sm text-muted-color">Listings you published to this marketplace.</div>
                </div>
                <p-button label="Publish plugin" icon="pi pi-plus" severity="success" routerLink="/publisher/plugins/new" />
            </div>

            <p-table [value]="listings" [loading]="loading" dataKey="manifest.id" [tableStyle]="{ 'min-width': '56rem' }">
                <ng-template #header>
                    <tr>
                        <th>Plugin</th>
                        <th>Version</th>
                        <th>Image</th>
                        <th>Install</th>
                        <th>Updated</th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template #body let-row>
                    <tr>
                        <td>
                            <div class="font-medium">{{ row.manifest.name }}</div>
                            <div class="text-sm text-muted-color">{{ row.manifest.id }} · {{ row.manifest.publisher }}</div>
                        </td>
                        <td>{{ row.manifest.version }}</td>
                        <td class="font-mono text-sm">{{ row.manifest.image }}</td>
                        <td>
                            <p-tag
                                [value]="row.installed ? (row.installState || 'Installed') : 'Not installed'"
                                [severity]="row.installed ? 'success' : 'secondary'"
                            />
                        </td>
                        <td>{{ row.updatedAt | date: 'short' }}</td>
                        <td>
                            <div class="flex gap-2 justify-end">
                                <p-button icon="pi pi-pencil" [rounded]="true" [outlined]="true" (onClick)="edit(row.manifest.id)" />
                                <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [outlined]="true" (onClick)="confirmUnpublish(row)" />
                            </div>
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="6" class="text-muted-color">No listings yet. Publish a plugin after pushing its Docker image.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>
    `
})
export class PublisherPlugins implements OnInit {
    private readonly api = inject(MarketplaceApi);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly router = inject(Router);
    private readonly messages = inject(MessageService);
    private readonly confirm = inject(ConfirmationService);

    listings: MarketplaceListingDto[] = [];
    loading = false;

    async ngOnInit() {
        await this.load();
    }

    async load() {
        this.loading = true;
        try {
            this.listings = await this.api.listMine();
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Load failed', detail: this.api.errorMessage(e) });
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    edit(pluginId: string) {
        void this.router.navigate(['/publisher/plugins', pluginId]);
    }

    confirmUnpublish(row: MarketplaceListingDto) {
        this.confirm.confirm({
            header: 'Unpublish listing',
            message: `Remove ${row.manifest.id} from the marketplace? Running containers are not uninstalled.`,
            acceptLabel: 'Unpublish',
            rejectLabel: 'Cancel',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => void this.unpublish(row.manifest.id)
        });
    }

    private async unpublish(pluginId: string) {
        try {
            await this.api.unpublish(pluginId);
            this.messages.add({ severity: 'success', summary: 'Unpublished', detail: pluginId });
            await this.load();
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Unpublish failed', detail: this.api.errorMessage(e) });
            this.cdr.detectChanges();
        }
    }
}
