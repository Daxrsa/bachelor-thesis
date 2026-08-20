import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TimelineModule } from 'primeng/timeline';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';
import { OrderResponse, ProductService } from '@/app/pages/service/product.service';

@Component({
    selector: 'app-store-my-orders',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule, TableModule, TagModule, TimelineModule],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-6xl mx-auto flex flex-col gap-3">
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <p-button icon="pi pi-arrow-left" label="Back to store" severity="secondary" [outlined]="true" [routerLink]="['/store']"></p-button>
                    <h3 class="m-0 text-2xl font-semibold">My orders</h3>
                    <p-button icon="pi pi-refresh" label="Refresh" variant="outlined" [loading]="loading" (onClick)="loadOrders()"></p-button>
                </div>

                <div *ngIf="!hasToken" class="card border border-amber-300 bg-amber-50 dark:bg-amber-900/20">
                    <p class="m-0 text-amber-700 dark:text-amber-300"><b>You are not logged in.</b> Please login to view your orders.</p>
                    <div class="mt-4"><p-button label="Login" icon="pi pi-sign-in" [routerLink]="['/auth/login']"></p-button></div>
                </div>

                <div *ngIf="hasToken && loading" class="card"><p class="m-0 text-surface-500 dark:text-surface-400">Loading your orders...</p></div>

                <div *ngIf="hasToken && error" class="card border border-red-300 bg-red-50 dark:bg-red-900/20">
                    <p class="m-0 text-red-700 dark:text-red-300"><b>Error:</b> {{ error }}</p>
                </div>

                <div *ngIf="hasToken && !loading && !error && orders.length === 0" class="card border border-surface-200 dark:border-surface-700">
                    <h3 class="m-0 text-xl font-semibold">No orders yet</h3>
                    <p class="text-surface-500 dark:text-surface-400">Paid orders will appear here after checkout completes.</p>
                    <div><p-button label="Go to products" icon="pi pi-arrow-right" [routerLink]="['/store/products']"></p-button></div>
                </div>

                <div *ngIf="hasToken && !loading && !error && orders.length > 0" class="card border border-surface-200 dark:border-surface-700">
                    <p-table [value]="orders" dataKey="id" responsiveLayout="scroll" [expandedRowKeys]="expandedOrderIds">
                        <ng-template pTemplate="header">
                            <tr>
                                <th style="width: 3rem"></th>
                                <th>Placed</th>
                                <th>Order</th>
                                <th>Items</th>
                                <th>Total</th>
                                <th>Status</th>
                                <th>Payment</th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-order let-expanded="expanded">
                            <tr>
                                <td><p-button [icon]="expanded ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" [text]="true" severity="secondary" (onClick)="toggleOrder(order.id)"></p-button></td>
                                <td>{{ order.createdAtUtc | date: 'medium' }}</td>
                                <td class="font-mono text-sm">{{ shortId(order.id) }}</td>
                                <td>{{ order.items.length }}</td>
                                <td class="font-semibold">{{ order.totalAmount | currency: order.currencyCode }}</td>
                                <td><p-tag [value]="normalizeStatus(order.status)" [severity]="statusSeverity(order.status)"></p-tag></td>
                                <td>{{ order.paymentMethod }}</td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="expandedrow" let-order>
                            <tr>
                                <td colspan="7" class="bg-surface-50 dark:bg-surface-900">
                                    <div class="p-3 flex flex-col gap-3">
                                        <p-timeline [value]="statusSteps" layout="horizontal" align="top">
                                            <ng-template #marker let-status>
                                                <span
                                                    class="flex w-8 h-8 items-center justify-center rounded-full text-white"
                                                    [ngClass]="isStatusReached(order.status, status) ? 'bg-primary' : 'bg-surface-400'"
                                                >
                                                    <i class="pi" [ngClass]="statusIcon(status)"></i>
                                                </span>
                                            </ng-template>
                                            <ng-template #content let-status>
                                                <span [class.font-semibold]="isCurrentStatus(order.status, status)">{{ status }}</span>
                                            </ng-template>
                                        </p-timeline>
                                        <div class="flex gap-x-6 gap-y-1 flex-wrap text-sm text-surface-600 dark:text-surface-300">
                                            <span>Paid {{ order.paidAtUtc | date: 'medium' }}</span>
                                            <span>Reference: {{ order.providerReference || order.paymentId }}</span>
                                        </div>
                                        <p-table [value]="order.items" responsiveLayout="scroll" size="small">
                                            <ng-template pTemplate="header"><tr><th>Product</th><th>Unit price</th><th>Quantity</th><th class="text-right">Line total</th></tr></ng-template>
                                            <ng-template pTemplate="body" let-item>
                                                <tr><td class="font-mono text-sm">{{ item.productId }}</td><td>{{ item.unitPrice | currency: order.currencyCode }}</td><td>{{ item.quantity }}</td><td class="text-right font-medium">{{ item.lineTotal | currency: order.currencyCode }}</td></tr>
                                            </ng-template>
                                        </p-table>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>
                </div>
            </section>
        </div>
    `
})
export class StoreMyOrders implements OnInit {
    private readonly productService = inject(ProductService);
    private readonly cdr = inject(ChangeDetectorRef);

    hasToken = false;
    loading = false;
    error = '';
    orders: OrderResponse[] = [];
    expandedOrderIds: Record<string, boolean> = {};
    readonly statusSteps = ['Processed', 'Accepted', 'Dispatched', 'Completed'];

    ngOnInit() {
        this.hasToken = !!localStorage.getItem(TOKEN_STORAGE_KEY);
        if (this.hasToken) void this.loadOrders();
    }

    async loadOrders() {
        if (!this.hasToken) return;
        this.loading = true;
        this.error = '';

        try {
            this.orders = (await this.productService.getMyOrders()) ?? [];
            this.expandedOrderIds = {};
        } catch (error: any) {
            this.error = error?.error?.error ?? error?.message ?? 'Failed to load orders.';
            this.orders = [];
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    toggleOrder(orderId: string) {
        this.expandedOrderIds = { ...this.expandedOrderIds, [orderId]: !this.expandedOrderIds[orderId] };
    }

    shortId(id: string) {
        return id.length > 12 ? `${id.slice(0, 12)}...` : id;
    }

    statusSeverity(status: string) {
        switch (this.normalizeStatus(status).toLowerCase()) {
            case 'completed':
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

    normalizeStatus(status: string) {
        return (status ?? '').toLowerCase() === 'paid' ? 'Processed' : status || 'Processed';
    }

    isCurrentStatus(orderStatus: string, step: string) {
        return this.normalizeStatus(orderStatus).toLowerCase() === step.toLowerCase();
    }

    isStatusReached(orderStatus: string, step: string) {
        const currentIndex = this.statusSteps.findIndex((value) => value.toLowerCase() === this.normalizeStatus(orderStatus).toLowerCase());
        const stepIndex = this.statusSteps.findIndex((value) => value.toLowerCase() === step.toLowerCase());
        return currentIndex >= 0 && stepIndex >= 0 && stepIndex <= currentIndex;
    }

    statusIcon(status: string) {
        switch (status.toLowerCase()) {
            case 'processed':
                return 'pi-inbox';
            case 'accepted':
                return 'pi-check';
            case 'dispatched':
                return 'pi-send';
            case 'completed':
                return 'pi-flag';
            default:
                return 'pi-circle';
        }
    }
}
