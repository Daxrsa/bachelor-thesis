import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { AddProductToCartPayload, Product, ProductService } from '@/app/pages/service/product.service';

@Component({
    selector: 'app-store-product-details',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, InputNumberModule, TagModule],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-7xl mx-auto flex flex-col gap-6">
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <p-button icon="pi pi-arrow-left" label="Back to products" severity="secondary" [outlined]="true" [routerLink]="['/store/products']"></p-button>
                    <span *ngIf="product?.code" class="text-xs md:text-sm text-surface-500 dark:text-surface-400">SKU: {{ product?.code }}</span>
                </div>

                <div *ngIf="loading" class="card">
                    <div class="grid grid-cols-12 gap-6 animate-pulse">
                        <div class="col-span-12 lg:col-span-5">
                            <div class="rounded-xl bg-surface-200 dark:bg-surface-700 h-[26rem]"></div>
                        </div>
                        <div class="col-span-12 lg:col-span-7 flex flex-col gap-4">
                            <div class="h-4 w-28 rounded bg-surface-200 dark:bg-surface-700"></div>
                            <div class="h-8 w-2/3 rounded bg-surface-200 dark:bg-surface-700"></div>
                            <div class="h-4 w-full rounded bg-surface-200 dark:bg-surface-700"></div>
                            <div class="h-4 w-5/6 rounded bg-surface-200 dark:bg-surface-700"></div>
                            <div class="h-28 w-full rounded-xl bg-surface-200 dark:bg-surface-700"></div>
                            <div class="h-40 w-full rounded-xl bg-surface-200 dark:bg-surface-700"></div>
                        </div>
                    </div>
                </div>

                <div *ngIf="error" class="card border border-red-300 bg-red-50 dark:bg-red-950/20">
                    <p class="m-0 text-red-700 dark:text-red-300"><b>Could not load product:</b> {{ error }}</p>
                </div>

                <div *ngIf="!loading && !error && product" class="grid grid-cols-12 gap-6">
                    <div class="col-span-12 lg:col-span-5">
                        <div class="card p-0 overflow-hidden border border-surface-200 dark:border-surface-700">
                            <div class="bg-gradient-to-br from-surface-50 to-surface-100 dark:from-surface-900 dark:to-surface-800 p-6">
                                <img class="rounded-xl w-full object-contain max-h-[30rem]" [src]="productImage(product)" [alt]="product.name || 'Product image'" />
                            </div>
                        </div>
                    </div>

                    <div class="col-span-12 lg:col-span-7">
                        <div class="card flex flex-col gap-5 border border-surface-200 dark:border-surface-700">
                            <div class="flex items-start justify-between gap-3 flex-wrap">
                                <div>
                                    <p class="m-0 text-surface-500 dark:text-surface-400 text-xs uppercase tracking-wide">{{ product.category || 'General' }}</p>
                                    <h1 class="m-0 text-3xl md:text-4xl font-semibold leading-tight">{{ product.name || 'Unnamed product' }}</h1>
                                </div>
                            </div>

                            <div class="flex items-end justify-between gap-3 flex-wrap">
                                <div>
                                    <p class="m-0 text-4xl font-semibold tracking-tight">{{ (product.price || 0) | currency: 'USD' }}</p>
                                    <p class="m-0 text-sm text-surface-500 dark:text-surface-400">Tax included. Shipping calculated at checkout.</p>
                                </div>
                                <div class="text-sm text-surface-500 dark:text-surface-400">
                                    Rating: <span class="font-semibold text-surface-900 dark:text-surface-0">{{ product.rating ?? 0 }}/5</span>
                                </div>
                            </div>

                            <p class="m-0 text-surface-700 dark:text-surface-200 leading-relaxed">
                                {{ product.description || 'No description available for this product yet.' }}
                            </p>

                            <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
                                <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-900/60">
                                    <p class="m-0 text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">Available</p>
                                    <p class="m-0 text-lg font-semibold">{{ product.quantity ?? 0 }}</p>
                                </div>
                                <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-900/60">
                                    <p class="m-0 text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">Product Code</p>
                                    <p class="m-0 text-lg font-semibold">{{ product.code || '-' }}</p>
                                </div>
                                <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-900/60">
                                    <p class="m-0 text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">Category</p>
                                    <p class="m-0 text-lg font-semibold">{{ product.category || 'General' }}</p>
                                </div>
                                <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-900/60">
                                    <p class="m-0 text-xs text-surface-500 dark:text-surface-400 uppercase tracking-wide">Status</p>
                                    <p class="m-0 text-lg font-semibold">{{ product.inventoryStatus || 'UNKNOWN' }}</p>
                                </div>
                            </div>

                            <div class="border-t border-surface-200 dark:border-surface-700 pt-4 mt-1 flex flex-col gap-3">
                                <div class="flex items-center justify-between gap-3 flex-wrap">
                                    <p class="m-0 text-sm text-surface-500 dark:text-surface-400">Add to cart</p>
                                    <span class="text-xs text-surface-500 dark:text-surface-400">Max {{ maxSelectableQuantity() }} units</span>
                                </div>

                                <div class="flex flex-col sm:flex-row sm:items-center gap-3">
                                    <div class="flex items-center gap-2">
                                        <label for="quantity" class="font-medium text-sm">Qty</label>
                                        <p-inputnumber
                                            inputId="quantity"
                                            [(ngModel)]="selectedQuantity"
                                            [min]="1"
                                            [max]="maxSelectableQuantity()"
                                            [showButtons]="true"
                                            [step]="1"
                                            [useGrouping]="false"
                                        ></p-inputnumber>
                                    </div>

                                    <p-button
                                        icon="pi pi-shopping-cart"
                                        label="Add to cart"
                                        [loading]="addingToCart"
                                        [disabled]="!canAddToCart()"
                                        (onClick)="addToCart()"
                                        styleClass="sm:ml-auto"
                                    ></p-button>
                                </div>

                                <div *ngIf="cartMessage" class="rounded-lg border border-green-200 bg-green-50 dark:bg-green-900/20 px-3 py-2 text-green-700 dark:text-green-300 text-sm">
                                    {{ cartMessage }}
                                </div>

                                <div *ngIf="cartError" class="rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 px-3 py-2 text-red-700 dark:text-red-300 text-sm">
                                    <b>Cart error:</b> {{ cartError }}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    `
})
export class StoreProductDetails implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly productService = inject(ProductService);
    private readonly cdr = inject(ChangeDetectorRef);

    loading = false;
    error = '';

    product: Product | null = null;

    selectedQuantity = 1;
    addingToCart = false;
    cartMessage = '';
    cartError = '';

    ngOnInit() {
        const id = this.route.snapshot.paramMap.get('id');
        if (!id) {
            this.error = 'Missing product id in route.';
            return;
        }

        void this.loadProduct(id);
    }

    async loadProduct(id: string) {
        this.loading = true;
        this.error = '';

        try {
            this.product = await this.productService.getStoreProductById(id);
            const available = this.maxSelectableQuantity();
            this.selectedQuantity = available > 0 ? 1 : 0;
        } catch (e: any) {
            this.error = e?.error?.error ?? e?.error?.title ?? e?.message ?? 'Failed to load product.';
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    canAddToCart() {
        const stock = this.maxSelectableQuantity();
        return !!this.product?.id && stock > 0 && this.selectedQuantity >= 1 && this.selectedQuantity <= stock;
    }

    maxSelectableQuantity() {
        return Math.max(0, this.product?.quantity ?? 0);
    }

    async addToCart() {
        if (!this.product?.id || !this.canAddToCart()) {
            return;
        }

        this.addingToCart = true;
        this.cartMessage = '';
        this.cartError = '';

        const payload: AddProductToCartPayload = {
            productId: this.product.id,
            quantity: this.selectedQuantity,
            cartId: null,
            cartItemId: null,
            correlationId: null
        };

        try {
            await this.productService.addProductToMyCart(payload);
            this.cartMessage = `${this.selectedQuantity} item(s) added to cart.`;
        } catch (e: any) {
            this.cartError = e?.error?.error ?? e?.error?.title ?? e?.message ?? 'Failed to add product to cart.';
        } finally {
            this.addingToCart = false;
            this.cdr.detectChanges();
        }
    }

    productImage(product: Product) {
        return this.productService.imageUrl(product) ?? `https://primefaces.org/cdn/primeng/images/demo/product/${product.image || 'placeholder.png'}`;
    }

    getSeverity(status?: string) {
        switch (status) {
            case 'INSTOCK':
                return 'success';
            case 'LOWSTOCK':
                return 'warn';
            case 'OUTOFSTOCK':
                return 'danger';
            default:
                return 'info';
        }
    }
}
