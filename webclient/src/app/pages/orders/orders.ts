import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { OrderResponse, ProductService } from '@/app/pages/service/product.service';

type TagSeverity = 'success' | 'warn' | 'danger' | 'info' | 'secondary';

@Component({
    selector: 'app-orders',
    standalone: true,
    imports: [CommonModule, FormsModule, SelectModule, TableModule, TagModule],
    providers: [ProductService],
    template: `
        <div class="card">
            <div class="mb-4">
                <div class="font-semibold text-xl">Orders</div>
                <div class="text-sm text-muted-color">All paid orders recorded by the order plugin.</div>
            </div>

            <div *ngIf="error()" class="text-red-500 mb-4">{{ error() }}</div>

            <p-table [value]="orders()" [loading]="loading()" dataKey="id" responsiveLayout="scroll" [tableStyle]="{ 'min-width': '72rem' }">
                <ng-template #header>
                    <tr>
                        <th>Placed</th>
                        <th>Order</th>
                        <th>User</th>
                        <th>Items</th>
                        <th>Total</th>
                        <th>Status</th>
                        <th>Payment</th>
                    </tr>
                </ng-template>
                <ng-template #body let-order>
                    <tr>
                        <td>{{ order.createdAtUtc | date: 'medium' }}</td>
                        <td class="font-mono text-sm">{{ order.id }}</td>
                        <td class="font-mono text-sm">{{ order.userId }}</td>
                        <td>{{ order.items.length }}</td>
                        <td class="font-semibold">{{ order.totalAmount | currency: order.currencyCode }}</td>
                        <td>
                            <p-select
                                [ngModel]="order.status"
                                [options]="statusOptions"
                                [disabled]="updatingOrderId() === order.id"
                                placeholder="Status"
                                appendTo="body"
                                [style]="{ 'min-width': '10rem' }"
                                (ngModelChange)="updateOrderStatus(order, $event)"
                            />
                        </td>
                        <td>{{ order.paymentMethod }}</td>
                    </tr>
                </ng-template>
                <ng-template #emptymessage>
                    <tr>
                        <td colspan="7">No orders recorded.</td>
                    </tr>
                </ng-template>
            </p-table>
        </div>
    `
})
export class Orders implements OnInit {
    readonly orders = signal<OrderResponse[]>([]);
    readonly loading = signal(false);
    readonly error = signal('');
    readonly updatingOrderId = signal('');
    readonly statusOptions = ['Processed', 'Accepted', 'Dispatched', 'Completed'];

    constructor(private readonly productService: ProductService) {}

    ngOnInit(): void {
        void this.loadOrders();
    }

    async loadOrders(): Promise<void> {
        this.loading.set(true);
        this.error.set('');

        try {
            this.orders.set((await this.productService.getAllOrders()) ?? []);
        } catch (error: any) {
            this.error.set(error?.error?.error ?? error?.message ?? 'Failed to load orders');
        } finally {
            this.loading.set(false);
        }
    }

    async updateOrderStatus(order: OrderResponse, status: string): Promise<void> {
        if (!status || status === order.status) {
            return;
        }

        this.updatingOrderId.set(order.id);
        this.error.set('');

        try {
            const updated = await this.productService.updateOrderStatus(order.id, status);
            this.orders.update((orders) => orders.map((item) => (item.id === updated.id ? updated : item)));
        } catch (error: any) {
            this.error.set(error?.error?.error ?? error?.message ?? 'Failed to update order status');
        } finally {
            this.updatingOrderId.set('');
        }
    }

    statusSeverity(status: string): TagSeverity {
        switch ((status ?? '').toLowerCase()) {
            case 'completed':
            case 'paid':
                return 'success';
            case 'dispatched':
            case 'accepted':
                return 'info';
            case 'processed':
                return 'warn';
            default:
                return 'secondary';
        }
    }
}
