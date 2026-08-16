import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';
import { CartItemResponse, CartResponse, Product, ProductService } from '@/app/pages/service/product.service';

interface CartRow {
    item: CartItemResponse;
    product: Product | null;
    removeQuantity: number;
}

@Component({
    selector: 'app-store-my-cart',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, CardModule, TableModule, InputNumberModule],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-6xl mx-auto flex flex-col gap-3">
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <p-button icon="pi pi-arrow-left" label="Back to store" severity="secondary" [outlined]="true" [routerLink]="['/store']"></p-button>
                    <p-button variant="outlined" label="Browse products" icon="pi pi-th-large" [routerLink]="['/store/products']"></p-button>
                </div>

                <div *ngIf="!hasToken" class="card border border-amber-300 bg-amber-50 dark:bg-amber-900/20">
                    <p class="m-0 text-amber-700 dark:text-amber-300"><b>You are not logged in.</b> Please login to view your cart.</p>
                    <div class="mt-4">
                        <p-button label="Login" icon="pi pi-sign-in" [routerLink]="['/auth/login']"></p-button>
                    </div>
                </div>

                <div *ngIf="hasToken && loading" class="card">
                    <p class="m-0 text-surface-500 dark:text-surface-400">Loading your cart...</p>
                </div>

                <div *ngIf="hasToken && error" class="card border border-red-300 bg-red-50 dark:bg-red-900/20">
                    <p class="m-0 text-red-700 dark:text-red-300"><b>Error:</b> {{ error }}</p>
                </div>

                <div *ngIf="hasToken && !loading && !error && cart && cart.items.length === 0" class="card border border-surface-200 dark:border-surface-700">
                    <h3 class="m-0 text-xl font-semibold">Your cart is empty</h3>
                    <p class="text-surface-500 dark:text-surface-400">Add products to your cart to see them here.</p>
                    <div>
                        <p-button label="Go to products" icon="pi pi-arrow-right" [routerLink]="['/store/products']"></p-button>
                    </div>
                </div>

                <div *ngIf="hasToken && !loading && !error && cart && cart.items.length > 0" class="card border border-surface-200 dark:border-surface-700">
                    <div class="flex items-center justify-between gap-3 flex-wrap">
                        <p class="m-0 text-surface-500 dark:text-surface-400"></p>
                    </div>

                    <p-table [value]="rows" responsiveLayout="scroll" [tableStyle]="{ width: '100%', 'table-layout': 'fixed' }">
                        <ng-template pTemplate="header">
                            <tr>
                                <th style="width: 32%">Product</th>
                                <th style="width: 12%">Unit Price</th>
                                <th style="width: 10%">Quantity</th>
                                <th style="width: 12%">Subtotal</th>
                                <th style="width: 16%">Added</th>
                                <th style="width: 20%; min-width: 280px">Actions</th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-row>
                            <tr>
                                <td>
                                    <div class="flex items-center gap-3">
                                        <img
                                            *ngIf="row.product"
                                            class="w-14 h-14 rounded object-cover border border-surface-200 dark:border-surface-700"
                                            [src]="productImage(row.product)"
                                            [alt]="row.product?.name || row.item.productId"
                                        />
                                        <div class="flex flex-col gap-1">
                                            <span class="font-medium">{{ row.product?.name || row.item.productId }}</span>
                                            <span class="text-sm text-surface-500 dark:text-surface-400">{{ row.product?.category || 'Unknown category' }}</span>
                                        </div>
                                    </div>
                                </td>
                                <td>{{ unitPrice(row) | currency: 'USD' }}</td>
                                <td>{{ row.item.quantity }}</td>
                                <td>{{ lineSubtotal(row) | currency: 'USD' }}</td>
                                <td>{{ row.item.createdAtUtc | date: 'medium' }}</td>
                                <td>
                                    <div class="flex flex-col gap-2">
                                        <div class="flex items-center gap-2 flex-wrap">
                                            <p-button
                                                icon="pi pi-external-link"
                                                severity="secondary"
                                                [outlined]="true"
                                                [routerLink]="['/store/products', row.item.productId]"
                                            ></p-button>

                                            <ng-container *ngIf="row.item.quantity > 1; else singleRemove">
                                                <p-inputnumber
                                                    [(ngModel)]="row.removeQuantity"
                                                    [min]="1"
                                                    [max]="row.item.quantity"
                                                    [showButtons]="true"
                                                    [step]="1"
                                                    [useGrouping]="false"
                                                    inputStyleClass="w-20"
                                                ></p-inputnumber>
                                                <p-button
                                                    icon="pi pi-trash"
                                                    severity="danger"
                                                    [loading]="removingProductId === row.item.productId"
                                                    (onClick)="removeItem(row)"
                                                ></p-button>
                                            </ng-container>

                                            <ng-template #singleRemove>
                                                <p-button
                                                    label="Remove"
                                                    icon="pi pi-trash"
                                                    severity="danger"
                                                    [loading]="removingProductId === row.item.productId"
                                                    (onClick)="removeItem(row)"
                                                ></p-button>
                                            </ng-template>
                                        </div>
                                        <small *ngIf="removeErrorByProduct[row.item.productId]" class="text-red-600">
                                            {{ removeErrorByProduct[row.item.productId] }}
                                        </small>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>

                    <div class="text-right flex gap-3 mt-4 flex-row justify-end">
                        <p class="m-0 font-semibold">{{ totalUnits() }} item(s)</p>
                        <p class="m-0 text-surface-500 dark:text-surface-400">Calculated total: {{ estimatedTotal() | currency: 'USD' }}</p>
                    </div>
                </div>
                <p-button
                    variant="outlined"
                    severity="help"
                    *ngIf="hasToken && cart && cart.items.length > 0"
                    icon="pi pi-credit-card"
                    label="Proceed to payment"
                    [routerLink]="['/store/checkout']"
                ></p-button>
            </section>
        </div>
    `
})
export class StoreMyCart implements OnInit {
    private readonly productService = inject(ProductService);
    private readonly cdr = inject(ChangeDetectorRef);

    hasToken = false;
    loading = false;
    error = '';
    cart: CartResponse | null = null;
    rows: CartRow[] = [];
    removingProductId = '';
    removeErrorByProduct: Record<string, string> = {};

    ngOnInit() {
        this.hasToken = !!localStorage.getItem(TOKEN_STORAGE_KEY);
        if (this.hasToken) {
            void this.loadCart();
        }
    }

    async loadCart() {
        if (!this.hasToken) {
            return;
        }

        this.loading = true;
        this.error = '';

        try {
            this.cart = await this.productService.getMyCart();
            this.rows = await this.hydrateRows(this.cart.items);
            this.removeErrorByProduct = {};
        } catch (e: any) {
            this.error = e?.error?.error ?? e?.message ?? 'Failed to load cart.';
            this.cart = null;
            this.rows = [];
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    async removeItem(row: CartRow) {
        const quantity = row.item.quantity > 1 ? Math.max(1, Math.min(row.item.quantity, row.removeQuantity || 1)) : 1;

        this.removingProductId = row.item.productId;
        this.removeErrorByProduct[row.item.productId] = '';

        try {
            await this.productService.removeProductFromMyCart(row.item.productId, quantity, this.cart?.id ?? null, null);
            await this.loadCart();
        } catch (e: any) {
            this.removeErrorByProduct[row.item.productId] = e?.error?.error ?? e?.message ?? 'Failed to remove item.';
        } finally {
            this.removingProductId = '';
            this.cdr.detectChanges();
        }
    }

    totalUnits() {
        return (this.cart?.items ?? []).reduce((sum: number, item: CartItemResponse) => sum + (item.quantity ?? 0), 0);
    }

    estimatedTotal() {
        return this.rows.reduce((sum, row) => sum + this.lineSubtotal(row), 0);
    }

    unitPrice(row: CartRow) {
        return row.product?.price ?? 0;
    }

    lineSubtotal(row: CartRow) {
        return this.unitPrice(row) * (row.item.quantity ?? 0);
    }

    productImage(product: Product) {
        return this.productService.imageUrl(product) ?? `https://primefaces.org/cdn/primeng/images/demo/product/${product.image || 'placeholder.png'}`;
    }

    private async hydrateRows(items: CartItemResponse[]) {
        const rows = await Promise.all(
            items.map(async (item) => {
                try {
                    const product = await this.productService.getStoreProductById(item.productId);
                    return { item, product, removeQuantity: 1 } satisfies CartRow;
                } catch {
                    return { item, product: null, removeQuantity: 1 } satisfies CartRow;
                }
            })
        );

        return rows;
    }
}
