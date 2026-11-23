import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface StoreBannerResponse {
    title: string;
    message: string;
    greeting: string;
}

@Component({
    selector: 'app-store-banner',
    standalone: true,
    imports: [CommonModule],
    template: `
        <section *ngIf="banner" class="relative overflow-hidden border-y border-amber-300 bg-amber-100 px-5 py-5 text-teal-950 md:px-8" aria-label="Store promotion">
            <div class="pointer-events-none absolute -right-12 -top-16 h-44 w-44 rounded-full border-[24px] border-amber-300/70"></div>
            <div class="relative flex flex-col gap-3 md:flex-row md:items-center md:justify-between md:gap-8">
                <div>
                    <p class="m-0 text-xs font-bold uppercase tracking-[0.16em] text-teal-700">{{ banner.title }}</p>
                    <p class="m-0 mt-1 text-xl font-semibold leading-tight md:text-2xl">{{ banner.message }}</p>
                </div>
                <p class="m-0 shrink-0 text-sm font-medium text-teal-800">{{ banner.greeting }}</p>
            </div>
        </section>
    `
})
export class StoreBanner implements OnInit {
    private readonly apiBase = 'http://localhost:8080';

    banner: StoreBannerResponse | null = null;

    constructor(
        private readonly http: HttpClient,
        private readonly changeDetectorRef: ChangeDetectorRef
    ) {}

    ngOnInit(): void {
        this.http.get<StoreBannerResponse>(`${this.apiBase}/api/p/banner-plugin/banner`).subscribe({
            next: (banner) => {
                this.banner = banner;
                this.changeDetectorRef.detectChanges();
            },
            error: () => {
                this.banner = null;
            }
        });
    }
}