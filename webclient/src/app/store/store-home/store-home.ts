import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CarouselModule } from 'primeng/carousel';
import { TagModule } from 'primeng/tag';
import { Product, ProductService } from '@/app/pages/service/product.service';

interface StoreCategory {
    name: string;
    count: number;
}

@Component({
    selector: 'app-store-home',
    standalone: true,
    imports: [CommonModule, RouterModule, ButtonModule, CarouselModule, TagModule],
    providers: [ProductService],
    template: `
        <main class="mx-auto max-w-[96rem] px-3 py-4 md:px-6 md:py-6 xl:px-8 xl:pb-16">
            <section class="relative grid min-h-[34rem] overflow-hidden border-t-[5px] border-orange-500 bg-emerald-50 px-6 py-12 md:px-12 lg:grid-cols-[1.2fr_0.8fr] lg:gap-12 lg:px-16 lg:py-20">
                <div class="relative z-10 flex max-w-2xl flex-col items-start justify-center">
                    <p class="mb-3 text-xs font-bold uppercase tracking-[0.16em] text-teal-700">A curated marketplace</p>
                    <h1 class="m-0 font-serif text-5xl font-medium leading-[0.95] text-teal-950 md:text-6xl xl:text-7xl">Ecommerce store</h1>
                    <p class="mt-6 max-w-xl text-lg leading-relaxed text-teal-900/80">A collection of products selected from a catalog</p>
                    <div class="mt-8 flex flex-wrap items-center gap-4">
                        <p-button label="Explore the collection" icon="pi pi-arrow-right" iconPos="right" [routerLink]="['/store/products']"></p-button>
                    </div>
                </div>

                <div class="relative min-h-72 lg:min-h-0" aria-hidden="true">
                    <div class="absolute inset-x-4 bottom-0 top-10 rounded-t-full bg-amber-300"></div>
                    <article *ngIf="heroProducts[0] as product" class="absolute right-[6%] top-4 h-80 w-52 rotate-3 overflow-hidden border-8 border-white bg-white shadow-2xl md:w-64 lg:h-[24rem]">
                        <img [src]="imageUrl(product)" [alt]="product.name || 'Featured product'" class="h-full w-full object-cover" />
                        <div class="absolute inset-x-0 bottom-0 bg-white/95 p-3">
                            <span class="block text-xs font-bold uppercase tracking-wider text-teal-700">{{ product.category || 'Collection' }}</span>
                            <strong class="block truncate text-sm text-teal-950">{{ product.name || 'Featured product' }}</strong>
                        </div>
                    </article>
                    <article *ngIf="heroProducts[1] as product" class="absolute bottom-1 left-0 h-36 w-28 -rotate-6 overflow-hidden border-8 border-white bg-white shadow-xl md:h-48 md:w-36">
                        <img [src]="imageUrl(product)" [alt]="product.name || 'Featured product'" class="h-full w-full object-cover" />
                    </article>
                    <div *ngIf="loading" class="absolute inset-0 grid place-items-center text-3xl text-teal-700"><i class="pi pi-spin pi-spinner"></i></div>
                </div>
            </section>

            <section class="pt-20">
                <div class="mb-8 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                    <div>
                        <h2 class="m-0 font-serif text-2xl font-medium leading-none text-teal-950">Discover the collection</h2>
                    </div>
                    <p *ngIf="!loading" class="m-0 max-w-xs text-sm leading-relaxed text-surface-500 md:text-right">{{ products.length }} products, fetched from product service.</p>
                </div>

                <div *ngIf="loading" class="grid min-h-56 place-items-center bg-surface-50 text-surface-500"><span><i class="pi pi-spin pi-spinner mr-2"></i>Loading products...</span></div>
                <div *ngIf="error" class="grid min-h-56 place-items-center bg-red-50 p-6 text-center text-red-700"><span><i class="pi pi-exclamation-circle mr-2"></i>{{ error }}</span></div>

                <p-carousel *ngIf="!loading && !error && products.length" [autoplayInterval]="2000" [circular]="true" [value]="products" [numVisible]="4" [numScroll]="1" [responsiveOptions]="carouselResponsiveOptions" [showNavigators]="true" [showIndicators]="false" styleClass="product-carousel">
                    <ng-template #item let-product>
                        <a class="group mx-2 flex min-w-0 flex-col text-inherit no-underline" [routerLink]="product.id ? ['/store/products', product.id] : ['/store/products']">
                            <div class="relative aspect-[1/1.08] overflow-hidden bg-surface-100">
                                <img [src]="imageUrl(product)" [alt]="product.name || 'Product image'" class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105" />
                                <p-tag [value]="inventoryLabel(product.inventoryStatus)" [severity]="getSeverity(product.inventoryStatus)" styleClass="absolute left-3 top-3"></p-tag>
                            </div>
                            <div class="flex flex-1 flex-col px-1 pt-4">
                                <span class="text-xs font-bold uppercase tracking-wider text-teal-700">{{ product.category || 'Uncategorized' }}</span>
                                <h3 class="mb-2 mt-1 min-h-11 font-sans text-base font-bold leading-snug text-teal-950">{{ product.name || 'Unnamed product' }}</h3>
                                <p class="mb-4 line-clamp-2 min-h-10 text-sm leading-relaxed text-surface-500">{{ product.description || 'Explore product details and availability.' }}</p>
                                <div class="mt-auto flex items-center justify-between gap-2">
                                    <strong class="text-lg text-teal-950">{{ (product.price || 0) | currency: 'USD' }}</strong>
                                    <span *ngIf="product.rating" class="text-sm font-bold text-amber-700"><i class="pi pi-star-fill mr-1 text-xs text-amber-500"></i>{{ product.rating | number: '1.1-1' }}</span>
                                </div>
                            </div>
                        </a>
                    </ng-template>
                </p-carousel>

                <div *ngIf="!loading && !error && !products.length" class="grid min-h-56 place-items-center bg-surface-50 p-6 text-center text-surface-500">
                    <div><i class="pi pi-box mb-3 block text-3xl text-teal-700"></i><h3 class="m-0 text-lg font-bold text-teal-950">The collection is being prepared.</h3><p class="mb-0 mt-2">Check back soon for products from the catalog.</p></div>
                </div>

            </section>
        </main>
    `
})
export class StoreHome implements OnInit {
    products: Product[] = [];
    loading = true;
    error = '';

    constructor(
        private readonly productService: ProductService,
        private readonly changeDetectorRef: ChangeDetectorRef
    ) {}

    carouselResponsiveOptions = [
        { breakpoint: '1280px', numVisible: 3, numScroll: 1 },
        { breakpoint: '900px', numVisible: 2, numScroll: 1 },
        { breakpoint: '600px', numVisible: 1, numScroll: 1 }
    ];

    get heroProducts(): Product[] {
        return this.products.slice(0, 2);
    }

    get categories(): StoreCategory[] {
        const counts = new Map<string, number>();
        for (const product of this.products) {
            const category = product.category?.trim() || 'Uncategorized';
            counts.set(category, (counts.get(category) ?? 0) + 1);
        }

        return [...counts.entries()]
            .map(([name, count]) => ({ name, count }))
            .sort((left, right) => right.count - left.count || left.name.localeCompare(right.name))
            .slice(0, 4);
    }

    async ngOnInit(): Promise<void> {
        try {
            this.products = await this.productService.getStoreProducts();
        } catch {
            this.error = 'We could not load the catalog right now. Please try again shortly.';
        } finally {
            this.loading = false;
            this.changeDetectorRef.detectChanges();
        }
    }

    imageUrl(product: Product): string {
        return this.productService.imageUrl(product) ?? '/demo/images/product/placeholder.svg';
    }

    inventoryLabel(status?: string): string {
        return status === 'INSTOCK' ? 'In stock' : status === 'LOWSTOCK' ? 'Low stock' : status === 'OUTOFSTOCK' ? 'Sold out' : 'Available';
    }

    getSeverity(status?: string): 'success' | 'warn' | 'danger' | 'info' {
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