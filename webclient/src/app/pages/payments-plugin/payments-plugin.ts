import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProductService, StripeWebhookResponse } from '@/app/pages/service/product.service';

type TagSeverity = 'success' | 'warn' | 'danger' | 'info' | 'secondary';

@Component({
    selector: 'app-payments-plugin',
    standalone: true,
    imports: [CommonModule, TableModule, TagModule],
    providers: [ProductService],
    template: `
        <div class="card">
            <div class="mb-4">
                <div class="font-semibold text-xl">Recorded Payments</div>
                <div class="text-sm text-muted-color">Payment activity recorded by the application.</div>
            </div>

            <div *ngIf="error()" class="text-red-500 mb-4">{{ error() }}</div>

            <p-table [value]="payments()" [loading]="loading()" dataKey="id" responsiveLayout="scroll" [tableStyle]="{ 'min-width': '64rem' }">
                <ng-template #header>
                    <tr>
                        <th>Payment ID</th>
                        <th>Event</th>
                        <th>Provider Reference</th>
                        <th>Customer</th>
                        <th>Amount</th>
                        <th>Status</th>
                        <th>Received</th>
                    </tr>
                </ng-template>
                <ng-template #body let-payment>
                    <tr>
                        <td class="font-medium">{{ payment.eventId }}</td>
                        <td>{{ payment.eventType }}</td>
                        <td>{{ payment.providerReference || '-' }}</td>
                        <td>{{ payment.customerEmail || payment.customerId || '-' }}</td>
                        <td>
                            {{ payment.amount == null ? '-' : (payment.amount | currency: payment.currencyCode || 'USD') }}
                        </td>
                        <td>
                            <p-tag [value]="payment.status || payment.eventType" [severity]="statusSeverity(payment.status || payment.eventType)" />
                        </td>
                        <td>{{ payment.receivedAtUtc | date: 'medium' }}</td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="7">No Stripe webhook events recorded.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>
    `
})
export class PaymentsPlugin implements OnInit {
    readonly payments = signal<StripeWebhookResponse[]>([]);
    readonly loading = signal(false);
    readonly error = signal('');

    constructor(private readonly productService: ProductService) {}

    ngOnInit(): void {
        void this.loadPayments();
    }

    async loadPayments(): Promise<void> {
        this.loading.set(true);
        this.error.set('');

        try {
            this.payments.set(await this.productService.getStripeWebhookEvents());
        } catch (error: any) {
            this.error.set(error?.error?.error ?? error?.message ?? 'Failed to load payments');
        } finally {
            this.loading.set(false);
        }
    }

    statusSeverity(status: string): TagSeverity {
        switch (status.toLowerCase()) {
            case 'succeeded':
                return 'success';
            case 'failed':
                return 'danger';
            case 'pending':
                return 'warn';
            default:
                return 'secondary';
        }
    }
}