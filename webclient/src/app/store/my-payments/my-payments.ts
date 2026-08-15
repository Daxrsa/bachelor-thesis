import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';
import { PaymentResponse, ProductService } from '@/app/pages/service/product.service';

@Component({
    selector: 'app-store-my-payments',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule, TableModule, TagModule],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-6xl mx-auto flex flex-col gap-3">
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <p-button icon="pi pi-arrow-left" label="Back to store" severity="secondary" [outlined]="true" [routerLink]="['/store']"></p-button>
                    <h3 class="m-0 text-2xl font-semibold">My payments</h3>
                    <p-button icon="pi pi-refresh" label="Refresh" variant="outlined" [loading]="loading" (onClick)="loadPayments()"></p-button>
                </div>

                <div *ngIf="!hasToken" class="card border border-amber-300 bg-amber-50 dark:bg-amber-900/20">
                    <p class="m-0 text-amber-700 dark:text-amber-300"><b>You are not logged in.</b> Please login to view your payments.</p>
                    <div class="mt-4">
                        <p-button label="Login" icon="pi pi-sign-in" [routerLink]="['/auth/login']"></p-button>
                    </div>
                </div>

                <div *ngIf="hasToken && loading" class="card">
                    <p class="m-0 text-surface-500 dark:text-surface-400">Loading your payments...</p>
                </div>

                <div *ngIf="hasToken && error" class="card border border-red-300 bg-red-50 dark:bg-red-900/20">
                    <p class="m-0 text-red-700 dark:text-red-300"><b>Error:</b> {{ error }}</p>
                </div>

                <div *ngIf="hasToken && !loading && !error && payments.length === 0" class="card border border-surface-200 dark:border-surface-700">
                    <h3 class="m-0 text-xl font-semibold">No payments yet</h3>
                    <p class="text-surface-500 dark:text-surface-400">Once you check out, your payments will appear here.</p>
                    <div>
                        <p-button label="Go to products" icon="pi pi-arrow-right" [routerLink]="['/store/products']"></p-button>
                    </div>
                </div>

                <div *ngIf="hasToken && !loading && !error && payments.length > 0" class="card border border-surface-200 dark:border-surface-700">
                    <p-table [value]="payments" responsiveLayout="scroll">
                        <ng-template pTemplate="header">
                            <tr>
                                <th>Date</th>
                                <th>Amount</th>
                                <th>Status</th>
                                <th>Method</th>
                                <th>Reference</th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-payment>
                            <tr>
                                <td>{{ payment.createdAtUtc | date: 'medium' }}</td>
                                <td>{{ payment.amount | currency: payment.currencyCode }}</td>
                                <td>
                                    <p-tag [value]="payment.status" [severity]="statusSeverity(payment.status)"></p-tag>
                                    <div *ngIf="payment.failureReason" class="text-sm text-red-600 mt-1">{{ payment.failureReason }}</div>
                                </td>
                                <td>{{ payment.paymentMethod }}</td>
                                <td class="text-sm text-surface-500 dark:text-surface-400">{{ payment.providerReference || payment.correlationId }}</td>
                            </tr>
                        </ng-template>
                    </p-table>
                </div>
            </section>
        </div>
    `
})
export class StoreMyPayments implements OnInit {
    private readonly productService = inject(ProductService);
    private readonly cdr = inject(ChangeDetectorRef);

    hasToken = false;
    loading = false;
    error = '';
    payments: PaymentResponse[] = [];

    ngOnInit() {
        this.hasToken = !!localStorage.getItem(TOKEN_STORAGE_KEY);
        if (this.hasToken) {
            void this.loadPayments();
        }
    }

    async loadPayments() {
        if (!this.hasToken) {
            return;
        }

        this.loading = true;
        this.error = '';

        try {
            this.payments = (await this.productService.getMyPayments()) ?? [];
        } catch (e: any) {
            this.error = e?.error?.error ?? e?.message ?? 'Failed to load payments.';
            this.payments = [];
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    statusSeverity(status: string) {
        switch ((status ?? '').toLowerCase()) {
            case 'succeeded':
            case 'success':
            case 'paid':
                return 'success';
            case 'failed':
            case 'canceled':
            case 'cancelled':
                return 'danger';
            case 'pending':
            case 'processing':
                return 'warn';
            default:
                return 'info';
        }
    }
}
