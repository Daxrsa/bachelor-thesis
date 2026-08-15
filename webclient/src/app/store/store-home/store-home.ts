import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CarouselModule } from 'primeng/carousel';
import { ChipModule } from 'primeng/chip';
import { DividerModule } from 'primeng/divider';
import { GalleriaModule } from 'primeng/galleria';
import { InputTextModule } from 'primeng/inputtext';
import { RatingModule } from 'primeng/rating';
import { TagModule } from 'primeng/tag';
import { Product, ProductService } from '@/app/pages/service/product.service';

interface StoreCategory {
    name: string;
    description: string;
    icon: string;
}

interface StoreBenefit {
    title: string;
    text: string;
    icon: string;
}

@Component({
    selector: 'app-store-home',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        CarouselModule,
        GalleriaModule,
        TagModule,
        ChipModule,
        RatingModule,
        DividerModule,
        InputTextModule
    ],
    providers: [ProductService],
    template: `
        <div class="w-full max-w-[95rem] mx-auto p-4 md:p-6 xl:p-8 flex flex-col gap-6">
            <p-card styleClass="overflow-hidden">
                <div class="grid grid-cols-12 gap-6 items-center">
                    <div class="col-span-12 lg:col-span-6 flex flex-col gap-4">
                        <h1 class="m-0 text-3xl md:text-5xl font-semibold leading-tight">Power Your Setup With Premium Electronics</h1>
                        <p class="m-0 text-surface-600 dark:text-surface-300 text-lg">Discover flagship phones, high-performance laptops, smart home devices, and pro audio gear with fast delivery and trusted warranties.</p>
                        <div class="flex flex-wrap gap-2">
                            <p-chip label="Free shipping over $99" icon="pi pi-truck"></p-chip>
                            <p-chip label="2-year warranty" icon="pi pi-shield"></p-chip>
                            <p-chip label="24/7 support" icon="pi pi-headphones"></p-chip>
                        </div>
                        <div class="flex flex-wrap gap-3">
                            <p-button label="Shop Products" icon="pi pi-arrow-right" [routerLink]="['/store/products']"></p-button>
                            <p-button label="View Deals" icon="pi pi-percentage" severity="secondary" [outlined]="true"></p-button>
                        </div>
                    </div>
                    <div class="col-span-12 lg:col-span-6">
                        <p-galleria
                            [value]="heroGalleryImages"
                            [responsiveOptions]="heroGalleriaResponsiveOptions"
                            [numVisible]="4"
                            [showThumbnails]="true"
                            thumbnailsPosition="bottom"
                            [showIndicators]="false"
                            [showItemNavigators]="false"
                            [circular]="true"
                            [autoPlay]="true"
                            [transitionInterval]="3500"
                            [containerStyle]="{ 'border-radius': '0.75rem', overflow: 'hidden' }"
                        >
                            <ng-template #item let-item>
                                <img [src]="item.itemImageSrc" [alt]="item.alt" class="w-full h-[24rem] object-cover" />
                            </ng-template>
                            <ng-template #thumbnail let-item>
                                <img [src]="item.itemImageSrc" [alt]="item.alt" class="w-full h-20 object-cover" />
                            </ng-template>
                        </p-galleria>
                    </div>
                </div>
            </p-card>

            <p-card>
                <ng-template #title>Shop By Category</ng-template>
                <ng-template #subtitle>Curated collections for every part of your setup</ng-template>

                <div class="grid grid-cols-12 gap-4 mt-2">
                    <div *ngFor="let category of categories" class="col-span-12 sm:col-span-6 xl:col-span-3">
                        <p-card styleClass="h-full">
                            <div class="flex flex-col gap-3 h-full">
                                <p-tag [value]="category.name" severity="contrast"></p-tag>
                                <div class="text-2xl"><i class="pi" [ngClass]="category.icon"></i></div>
                                <p class="m-0 text-surface-600 dark:text-surface-300">{{ category.description }}</p>
                                <div class="mt-auto">
                                    <p-button label="Browse" severity="secondary" [outlined]="true" [routerLink]="['/store/products']"></p-button>
                                </div>
                            </div>
                        </p-card>
                    </div>
                </div>
            </p-card>

            <p-card>
                <ng-template #title>Featured Electronics</ng-template>
                <ng-template #subtitle>Handpicked devices with top customer ratings</ng-template>

                <p-carousel [value]="featuredProducts" [numVisible]="4" [numScroll]="1" [circular]="false" [responsiveOptions]="carouselResponsiveOptions">
                    <ng-template #item let-product>
                        <div class="p-2">
                            <p-card styleClass="h-full">
                                <div class="flex flex-col gap-3 h-full">
                                    <div class="relative">
                                        <img [src]="productImage(product)" [alt]="product.name || 'Product'" class="w-full rounded-lg" />
                                        <div class="absolute" [ngStyle]="{ left: '8px', top: '8px' }">
                                            <p-tag [value]="product.inventoryStatus || 'INSTOCK'" [severity]="getSeverity(product.inventoryStatus)"></p-tag>
                                        </div>
                                    </div>
                                    <div>
                                        <div class="text-surface-500 dark:text-surface-400 text-sm">{{ product.category || 'Electronics' }}</div>
                                        <div class="text-lg font-medium">{{ product.name || 'Unnamed Product' }}</div>
                                    </div>
                                    <p-rating [ngModel]="product.rating || 0" [readonly]="true"></p-rating>
                                    <div class="flex items-center justify-between mt-auto">
                                        <div class="text-2xl font-semibold">{{ (product.price || 0) | currency: 'USD' }}</div>
                                        <p-button icon="pi pi-shopping-cart" [disabled]="product.inventoryStatus === 'OUTOFSTOCK'"></p-button>
                                    </div>
                                </div>
                            </p-card>
                        </div>
                    </ng-template>
                </p-carousel>
            </p-card>

            <p-card>
                <ng-template #title>Why Customers Choose NEXTRONICS</ng-template>
                <div class="grid grid-cols-12 gap-4 mt-2">
                    <div *ngFor="let benefit of benefits" class="col-span-12 md:col-span-4">
                        <p-card styleClass="h-full">
                            <div class="flex flex-col gap-2">
                                <p-tag [value]="benefit.title" severity="success"></p-tag>
                                <div class="text-2xl"><i class="pi" [ngClass]="benefit.icon"></i></div>
                                <p class="m-0 text-surface-600 dark:text-surface-300">{{ benefit.text }}</p>
                            </div>
                        </p-card>
                    </div>
                </div>

                <p-divider></p-divider>

                <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                    <div>
                        <h3 class="m-0 text-xl font-semibold">Get Weekly Tech Drops</h3>
                        <p class="m-0 text-surface-500 dark:text-surface-400">Deals, product launches, and members-only promo codes.</p>
                    </div>
                    <div class="flex gap-2 w-full md:w-auto">
                        <input pInputText placeholder="Enter your email" class="w-full md:w-80" [(ngModel)]="newsletterEmail" />
                        <p-button label="Subscribe" icon="pi pi-send"></p-button>
                    </div>
                </div>
            </p-card>
        </div>
    `
})
export class StoreHome implements OnInit {
    newsletterEmail = '';

    featuredProducts: Product[] = [];

    heroGalleryImages: Array<{ itemImageSrc: string; alt: string }> = [];

    categories: StoreCategory[] = [
        { name: 'Laptops', description: 'Ultrabooks, gaming rigs, and creator-grade machines.', icon: 'pi-desktop' },
        { name: 'Smartphones', description: 'Latest flagship phones with advanced camera systems.', icon: 'pi-mobile' },
        { name: 'Audio', description: 'Wireless earbuds, headsets, and immersive home audio.', icon: 'pi-headphones' },
        { name: 'Gaming', description: 'Consoles, controllers, and RGB-ready accessories.', icon: 'pi-microchip' }
    ];

    benefits: StoreBenefit[] = [
        { title: 'Certified Devices', text: 'Every product is tested and sourced from authorized distributors.', icon: 'pi-verified' },
        { title: 'Fast Fulfillment', text: 'Same-day dispatch for most products with live tracking updates.', icon: 'pi-send' },
        { title: 'Expert Support', text: 'Real tech specialists help with setup, upgrades, and troubleshooting.', icon: 'pi-comments' }
    ];

    carouselResponsiveOptions = [
        {
            breakpoint: '1280px',
            numVisible: 3,
            numScroll: 1
        },
        {
            breakpoint: '1024px',
            numVisible: 2,
            numScroll: 1
        },
        {
            breakpoint: '640px',
            numVisible: 1,
            numScroll: 1
        }
    ];

    heroGalleriaResponsiveOptions = [
        {
            breakpoint: '1024px',
            numVisible: 2
        },
        {
            breakpoint: '640px',
            numVisible: 1
        }
    ];

    constructor(private productService: ProductService) {}

    ngOnInit() {
        this.productService.getStoreProducts().then((products) => {
            const electronics = products.filter((product) => product.category === 'Electronics');
            const source = electronics.length ? electronics : products;

            this.featuredProducts = source.slice(0, 8);
            this.heroGalleryImages = products.slice(0, 14).map((product) => ({
                itemImageSrc: this.productImage(product),
                alt: product.name || 'Electronics product'
            }));
        });
    }

    productImage(product: Product) {
        return `https://primefaces.org/cdn/primeng/images/demo/product/${product.image || 'placeholder.png'}`;
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
