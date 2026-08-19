import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DataViewModule } from 'primeng/dataview';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SliderModule } from 'primeng/slider';
import { TagModule } from 'primeng/tag';
import { Product, ProductService } from '@/app/pages/service/product.service';

interface ProductAvailabilityOption {
    label: string;
    value: string;
}

interface ProductFilters {
    priceRange: number[];
    availability: string[];
}

const PRODUCT_AVAILABILITY_OPTIONS: ProductAvailabilityOption[] = [
    { label: 'In stock', value: 'INSTOCK' },
    { label: 'Low stock', value: 'LOWSTOCK' },
    { label: 'Out of stock', value: 'OUTOFSTOCK' }
];

function createDefaultProductFilters(): ProductFilters {
    return {
        priceRange: [0, 0],
        availability: PRODUCT_AVAILABILITY_OPTIONS.map((option) => option.value)
    };
}

@Component({
    selector: 'app-product-filter-sidebar',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, SliderModule],
    template: `
        <aside class="flex flex-col gap-6 border border-surface-200 bg-surface-0 p-5 shadow-sm dark:border-surface-700 dark:bg-surface-900 lg:sticky lg:top-24">
            <div class="flex justify-end border-b border-surface-200 pb-3 dark:border-surface-700">
                <p-button icon="pi pi-refresh" ariaLabel="Reset filters" severity="secondary" [text]="true" size="small" (onClick)="resetFilters()"></p-button>
            </div>

            <div class="flex flex-col gap-4">
                <div class="flex items-center justify-between gap-3">
                    <span class="font-bold text-surface-900 dark:text-surface-0">Price</span>
                    <span class="text-xs font-medium text-surface-500">USD</span>
                </div>
                <div class="grid grid-cols-[1fr_auto_1fr] items-center gap-2">
                    <div class="rounded-md border border-surface-200 bg-surface-50 px-3 py-2 text-sm font-semibold text-surface-900 dark:border-surface-700 dark:bg-surface-800 dark:text-surface-0">{{ priceRange[0] | currency: 'USD' : 'symbol' : '1.0-0' }}</div>
                    <span class="text-xs text-surface-400">to</span>
                    <div class="rounded-md border border-surface-200 bg-surface-50 px-3 py-2 text-right text-sm font-semibold text-surface-900 dark:border-surface-700 dark:bg-surface-800 dark:text-surface-0">{{ priceRange[1] | currency: 'USD' : 'symbol' : '1.0-0' }}</div>
                </div>
                <p-slider [(ngModel)]="priceRange" [range]="true" [min]="priceBounds[0]" [max]="priceBounds[1]" (ngModelChange)="onPriceRangeChange()" styleClass="my-1"></p-slider>
                <div class="flex justify-between text-xs text-surface-500 dark:text-surface-400"><span>Min {{ priceBounds[0] | currency: 'USD' : 'symbol' : '1.0-0' }}</span><span>Max {{ priceBounds[1] | currency: 'USD' : 'symbol' : '1.0-0' }}</span></div>
            </div>

            <div class="flex flex-col gap-3 border-t border-surface-200 pt-5 dark:border-surface-700">
                <span class="font-bold text-surface-900 dark:text-surface-0">Availability</span>
                <label *ngFor="let option of availabilityOptions" class="flex cursor-pointer items-center gap-3 rounded-md border border-transparent px-3 py-2.5 transition-colors hover:bg-surface-50 dark:hover:bg-surface-800" [ngClass]="{ 'border-teal-200 bg-teal-50 dark:border-teal-800 dark:bg-teal-950': selectedAvailability.includes(option.value) }">
                    <input type="checkbox" class="h-4 w-4 accent-teal-700" [checked]="selectedAvailability.includes(option.value)" (change)="toggleAvailability(option.value, $any($event.target).checked)" />
                    <span class="flex-1 text-sm font-medium">{{ option.label }}</span>
                    <i *ngIf="selectedAvailability.includes(option.value)" class="pi pi-check text-xs text-teal-700"></i>
                </label>
            </div>
        </aside>
    `
})
export class ProductFilterSidebar implements OnChanges {
    @Input() filters: ProductFilters = createDefaultProductFilters();

    @Input() priceBounds: number[] = [0, 0];

    @Output() filtersChange = new EventEmitter<ProductFilters>();

    availabilityOptions = PRODUCT_AVAILABILITY_OPTIONS;

    priceRange: number[] = [0, 0];

    selectedAvailability: string[] = PRODUCT_AVAILABILITY_OPTIONS.map((option) => option.value);

    ngOnChanges(changes: SimpleChanges) {
        if (changes['filters'] || changes['priceBounds']) {
            this.priceRange = [...this.filters.priceRange];
            this.selectedAvailability = [...this.filters.availability];
        }
    }

    onPriceRangeChange() {
        if (this.areRangesEqual(this.priceRange, this.filters.priceRange)) {
            return;
        }

        this.emitFilters();
    }

    toggleAvailability(value: string, checked: boolean) {
        this.selectedAvailability = checked ? [...this.selectedAvailability, value] : this.selectedAvailability.filter((status) => status !== value);
        this.emitFilters();
    }

    resetFilters() {
        this.priceRange = [...this.priceBounds];
        this.selectedAvailability = PRODUCT_AVAILABILITY_OPTIONS.map((option) => option.value);
        this.emitFilters();
    }

    emitFilters() {
        this.filtersChange.emit({
            priceRange: [...this.priceRange],
            availability: [...this.selectedAvailability]
        });
    }

    private areRangesEqual(left: number[], right: number[]) {
        return left.length === right.length && left.every((value, index) => value === right[index]);
    }
}

@Component({
    selector: 'app-store-products',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, DataViewModule, SelectButtonModule, TagModule, ProductFilterSidebar],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-7xl mx-auto">

                <div *ngIf="error" class="card border border-red-300 text-red-600 mb-6">
                    <b>Error:</b> {{ error }}
                </div>

                <div class="grid grid-cols-12 gap-6 items-start">
                    <div class="col-span-12 lg:col-span-3">
                        <app-product-filter-sidebar [filters]="filters" [priceBounds]="priceBounds" (filtersChange)="onFiltersChange($event)"></app-product-filter-sidebar>
                    </div>

                    <div class="col-span-12 lg:col-span-9">
                        <div class="border border-surface-200 bg-surface-0 p-2 shadow-sm dark:border-surface-700 dark:bg-surface-900 md:p-4">
                            <p-dataview [value]="filteredProducts" [layout]="layout">
                                <ng-template #header>
                                    <div class="mb-2 flex flex-col gap-3 border-b border-surface-200 pb-4 sm:flex-row sm:items-center sm:justify-between dark:border-surface-700">
                                        <span class="text-sm font-medium text-surface-500 dark:text-surface-400">Showing <strong class="text-surface-900 dark:text-surface-0">{{ filteredProducts.length }}</strong> of {{ products.length }} products</span>
                                        <p-select-button [(ngModel)]="layout" [options]="layoutOptions" [allowEmpty]="false" styleClass="shrink-0">
                                            <ng-template #item let-option>
                                                <i class="pi" [ngClass]="{ 'pi-bars': option === 'list', 'pi-table': option === 'grid' }"></i>
                                            </ng-template>
                                        </p-select-button>
                                    </div>
                                </ng-template>

                                <ng-template #list let-items>
                                    <div class="flex flex-col">
                                        <div *ngFor="let item of items; let i = index">
                                            <div class="flex flex-col sm:flex-row sm:items-center p-6 gap-4" [ngClass]="{ 'border-t border-surface': i !== 0 }">
                                                <div class="md:w-40 relative">
                                                    <img class="block xl:block mx-auto rounded w-full" [src]="imageUrl(item)" [alt]="item.name" />
                                                    <div class="absolute bg-black/70 rounded-border" [style]="{ left: '4px', top: '4px' }">
                                                        <p-tag [value]="item.inventoryStatus" [severity]="getSeverity(item)"></p-tag>
                                                    </div>
                                                </div>
                                                <div class="flex flex-col md:flex-row justify-between md:items-center flex-1 gap-6">
                                                    <div class="flex flex-row md:flex-col justify-between items-start gap-2">
                                                        <div>
                                                            <span class="font-medium text-surface-500 dark:text-surface-400 text-sm">{{ item.category }}</span>
                                                            <div class="text-lg font-medium mt-2">{{ item.name }}</div>
                                                        </div>
                                                        <div class="bg-surface-100 p-1" style="border-radius: 30px">
                                                            <div
                                                                class="bg-surface-0 flex items-center gap-2 justify-center py-1 px-2"
                                                                style="
                                                            border-radius: 30px;
                                                            box-shadow:
                                                                0px 1px 2px 0px rgba(0, 0, 0, 0.04),
                                                                0px 1px 2px 0px rgba(0, 0, 0, 0.06);
                                                        "
                                                            >
                                                                <span class="text-surface-900 font-medium text-sm">{{ item.rating }}</span>
                                                                <i class="pi pi-star-fill text-yellow-500"></i>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="flex flex-col md:items-end gap-8">
                                                        <span class="text-xl font-semibold">$ {{ item.price }}</span>
                                                        <div class="flex flex-row-reverse md:flex-row gap-2">
                                                            <p-button icon="pi pi-heart" styleClass="h-full" [outlined]="true"></p-button>
                                                            <p-button
                                                                icon="pi pi-shopping-cart"
                                                                label="Buy Now"
                                                                [disabled]="item.inventoryStatus === 'OUTOFSTOCK' || !item.id"
                                                                [routerLink]="item.id ? ['/store/products', item.id] : null"
                                                                styleClass="flex-auto md:flex-initial whitespace-nowrap"
                                                            ></p-button>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ng-template>

                                <ng-template #grid let-items>
                                    <div class="grid grid-cols-12 ">
                                        <div *ngFor="let item of items" class="col-span-12 sm:col-span-6 lg:col-span-4 p-2">
                                            <div class="p-4 border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 rounded flex flex-col h-full">
                                                <div class="relative w-full shadow-sm">
                                                    <img class="rounded w-full" [src]="imageUrl(item)" [alt]="item.name" />
                                                    <div class="absolute bg-black/70 rounded-border" [style]="{ left: '4px', top: '4px' }">
                                                        <p-tag [value]="item.inventoryStatus" [severity]="getSeverity(item)"></p-tag>
                                                    </div>
                                                </div>
                                                <div class="pt-12 flex flex-col h-full">
                                                    <div class="flex flex-row justify-between items-start gap-2">
                                                        <div>
                                                            <span class="font-medium text-surface-500 dark:text-surface-400 text-sm">{{ item.category }}</span>
                                                            <div class="text-lg font-medium mt-1">{{ item.name }}</div>
                                                        </div>
                                                        <div class="bg-surface-100 p-1" style="border-radius: 30px">
                                                            <div
                                                                class="bg-surface-0 flex items-center gap-2 justify-center py-1 px-2"
                                                                style="
                                                            border-radius: 30px;
                                                            box-shadow:
                                                                0px 1px 2px 0px rgba(0, 0, 0, 0.04),
                                                                0px 1px 2px 0px rgba(0, 0, 0, 0.06);
                                                        "
                                                            >
                                                                <span class="text-surface-900 font-medium text-xs">{{ item.rating }}</span>
                                                                <i class="pi pi-star-fill text-yellow-500"></i>
                                                            </div>
                                                        </div>
                                                    </div>

                                                    <div class="flex flex-col gap-6 mt-6 mt-auto">
                                                        <span class="text-2xl font-semibold">$ {{ item.price }}</span>
                                                        <div class="flex gap-2">
                                                            <p-button
                                                                icon="pi pi-shopping-cart"
                                                                label="Buy Now"
                                                                [disabled]="item.inventoryStatus === 'OUTOFSTOCK' || !item.id"
                                                                [routerLink]="item.id ? ['/store/products', item.id] : null"
                                                                class="flex-auto whitespace-nowrap"
                                                                styleClass="w-full"
                                                            ></p-button>
                                                            <p-button icon="pi pi-heart" styleClass="h-full" [outlined]="true"></p-button>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ng-template>
                            </p-dataview>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    `
})
export class StoreProducts implements OnInit {
    layout: 'list' | 'grid' = 'grid';

    layoutOptions: Array<'list' | 'grid'> = ['list', 'grid'];

    products: Product[] = [];

    filteredProducts: Product[] = [];

    filters: ProductFilters = createDefaultProductFilters();

    priceBounds: number[] = [0, 0];

    loading = true;

    error = '';

    constructor(readonly productService: ProductService, private cdr: ChangeDetectorRef) {}

    ngOnInit() {
        // Defer initial fetch to the next macrotask so bindings stay stable during first dev-mode checks.
        setTimeout(() => {
            void this.loadProducts();
        }, 0);
    }

    async loadProducts() {
        this.loading = true;
        this.error = '';

        try {
            const data = await this.productService.getStoreProducts();
            console.log('Products fetched from API:', data);
            this.products = data;
            this.priceBounds = this.getPriceBounds(data);
            this.filters = { ...this.filters, priceRange: [...this.priceBounds] };
            this.applyFilters();
        } catch (e: any) {
            this.error = e?.error?.error ?? e?.message ?? 'Failed to load products';
            this.products = [];
            this.filteredProducts = [];
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    onFiltersChange(filters: ProductFilters) {
        if (this.areFiltersEqual(this.filters, filters)) {
            return;
        }

        this.filters = filters;
        this.applyFilters();
    }

    applyFilters() {
        const [minimumPrice, maximumPrice] = this.filters.priceRange;

        this.filteredProducts = this.products.filter((product) => {
            const price = product.price ?? 0;
            const availability = product.inventoryStatus ?? '';

            return price >= minimumPrice && price <= maximumPrice && this.filters.availability.includes(availability);
        });
    }

    getPriceBounds(products: Product[]) {
        const prices = products.map((product) => product.price).filter((price): price is number => typeof price === 'number');

        if (!prices.length) {
            return [0, 0];
        }

        return [Math.min(...prices), Math.max(...prices)];
    }

    getSeverity(product: Product) {
        switch (product.inventoryStatus) {
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

    imageUrl(product: Product): string {
        return this.productService.imageUrl(product) ?? `https://primefaces.org/cdn/primevue/images/product/${product.image}`;
    }

    private areFiltersEqual(left: ProductFilters, right: ProductFilters) {
        const samePrice = left.priceRange.length === right.priceRange.length && left.priceRange.every((value, index) => value === right.priceRange[index]);
        const sameAvailability = left.availability.length === right.availability.length && left.availability.every((value, index) => value === right.availability[index]);
        return samePrice && sameAvailability;
    }
}
