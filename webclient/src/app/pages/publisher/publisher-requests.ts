import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { MarketplaceApi, PublisherRequestDto } from './marketplace.api';

@Component({
    selector: 'app-publisher-requests',
    standalone: true,
    imports: [CommonModule, TableModule, ButtonModule, ToastModule],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <div class="flex items-center justify-between mb-4">
                <div>
                    <div class="font-semibold text-xl">Publisher requests</div>
                    <div class="text-sm text-muted-color">Approve authors so they can list plugins. They must sign in again after approval.</div>
                </div>
                <p-button label="Refresh" icon="pi pi-refresh" severity="secondary" [loading]="loading" (onClick)="load()" />
            </div>

            <p-table [value]="requests" [loading]="loading" dataKey="id">
                <ng-template #header>
                    <tr>
                        <th>Email</th>
                        <th>Message</th>
                        <th>Requested</th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template #body let-row>
                    <tr>
                        <td>{{ row.email }}</td>
                        <td>{{ row.message || '—' }}</td>
                        <td>{{ row.createdAt | date: 'medium' }}</td>
                        <td>
                            <div class="flex gap-2 justify-end">
                                <p-button label="Approve" icon="pi pi-check" severity="success" [loading]="busyId === row.id" (onClick)="approve(row)" />
                                <p-button label="Reject" icon="pi pi-times" severity="danger" [loading]="busyId === row.id" (onClick)="reject(row)" />
                            </div>
                        </td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="4" class="text-muted-color">No pending requests.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>
    `
})
export class PublisherRequests implements OnInit {
    private readonly api = inject(MarketplaceApi);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly messages = inject(MessageService);

    requests: PublisherRequestDto[] = [];
    loading = false;
    busyId = '';

    ngOnInit() {
        void this.load();
    }

    async load() {
        this.loading = true;
        try {
            this.requests = await this.api.pendingPublisherRequests();
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Load failed', detail: this.api.errorMessage(e) });
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    async approve(row: PublisherRequestDto) {
        this.busyId = row.id;
        try {
            await this.api.approvePublisherRequest(row.id);
            this.messages.add({ severity: 'success', summary: 'Approved', detail: row.email });
            await this.load();
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Approve failed', detail: this.api.errorMessage(e) });
        } finally {
            this.busyId = '';
            this.cdr.detectChanges();
        }
    }

    async reject(row: PublisherRequestDto) {
        this.busyId = row.id;
        try {
            await this.api.rejectPublisherRequest(row.id);
            this.messages.add({ severity: 'info', summary: 'Rejected', detail: row.email });
            await this.load();
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Reject failed', detail: this.api.errorMessage(e) });
        } finally {
            this.busyId = '';
            this.cdr.detectChanges();
        }
    }
}
