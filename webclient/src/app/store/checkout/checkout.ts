import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { firstValueFrom } from 'rxjs';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';
import { CartItemResponse, CartResponse, Product, ProductService, PaymentResponse } from '@/app/pages/service/product.service';

interface CheckoutRow {
    item: CartItemResponse;
    product: Product | null;
}

@Component({
    selector: 'app-store-checkout',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, InputTextModule, MessageModule, SelectModule],
    providers: [ProductService],
    template: `
        <div class="p-4 md:p-6 xl:p-8">
            <section class="max-w-6xl mx-auto flex flex-col gap-4">
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <p-button icon="pi pi-arrow-left" label="Back to cart" severity="secondary" [outlined]="true" [routerLink]="['/store/my-cart']"></p-button>
                    <h3 class="m-0 text-2xl font-semibold">Checkout</h3>
                </div>

                <div *ngIf="!hasToken" class="card border border-amber-300 bg-amber-50 dark:bg-amber-900/20">
                    <p class="m-0 text-amber-700 dark:text-amber-300"><b>You are not logged in.</b> Please login to pay for your cart.</p>
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

                <div *ngIf="hasToken && !loading && !error && rows.length === 0 && !paid" class="card border border-surface-200 dark:border-surface-700">
                    <h3 class="m-0 text-xl font-semibold">Nothing to pay for</h3>
                    <p class="text-surface-500 dark:text-surface-400">Your cart is empty.</p>
                    <div>
                        <p-button label="Go to products" icon="pi pi-arrow-right" [routerLink]="['/store/products']"></p-button>
                    </div>
                </div>

                <div *ngIf="paid" class="card border border-green-300 bg-green-50 dark:bg-green-900/20">
                    <h3 class="m-0 text-xl font-semibold text-green-700 dark:text-green-300">Payment successful</h3>
                    <p class="text-green-700 dark:text-green-300">
                        Charged {{ total() | currency: 'USD' }} to card ending in {{ last4 }}. Reference: <b>{{ reference }}</b>
                    </p>
                    <div class="mt-4">
                        <p-button label="Back to store" icon="pi pi-home" [routerLink]="['/store']"></p-button>
                    </div>
                </div>

                <div *ngIf="hasToken && !loading && !error && rows.length > 0 && !paid" class="grid grid-cols-12 gap-6">
                    <div class="col-span-12 lg:col-span-7">
                        <form #paymentForm="ngForm" class="card border border-surface-200 dark:border-surface-700 flex flex-col gap-4" (ngSubmit)="pay(paymentForm)" novalidate>
                            <h4 class="m-0 text-xl font-semibold">Card details</h4>

                            <div class="flex flex-col gap-2">
                                <label for="cardName" class="font-medium text-sm">Name on card</label>
                                <input pInputText id="cardName" name="cardName" [(ngModel)]="cardName" autocomplete="cc-name" class="w-full" />
                                <small *ngIf="submitted && !cardNameValid()" class="text-red-600">Enter the name printed on the card.</small>
                            </div>

                            <div class="flex flex-col gap-2">
                                <label for="cardNumber" class="font-medium text-sm">Card number</label>
                                <div class="relative">
                                    <input
                                        pInputText
                                        id="cardNumber"
                                        name="cardNumber"
                                        inputmode="numeric"
                                        autocomplete="cc-number"
                                        placeholder="0000 0000 0000 0000"
                                        class="w-full pr-24"
                                        [ngModel]="cardNumber"
                                        (ngModelChange)="onCardNumberChange($event)"
                                    />
                                    <span class="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-surface-500 dark:text-surface-400">{{ cardBrand() }}</span>
                                </div>
                                <small *ngIf="submitted && !cardNumberValid()" class="text-red-600">Enter a valid card number.</small>
                            </div>

                            <div class="grid grid-cols-12 gap-3">
                                <div class="col-span-6 sm:col-span-4 flex flex-col gap-2">
                                    <label for="expMonth" class="font-medium text-sm">Month</label>
                                    <p-select inputId="expMonth" name="expMonth" [options]="months" [(ngModel)]="expMonth" placeholder="MM" styleClass="w-full"></p-select>
                                </div>
                                <div class="col-span-6 sm:col-span-4 flex flex-col gap-2">
                                    <label for="expYear" class="font-medium text-sm">Year</label>
                                    <p-select inputId="expYear" name="expYear" [options]="years" [(ngModel)]="expYear" placeholder="YYYY" styleClass="w-full"></p-select>
                                </div>
                                <div class="col-span-12 sm:col-span-4 flex flex-col gap-2">
                                    <label for="cvv" class="font-medium text-sm">CVV</label>
                                    <input
                                        pInputText
                                        id="cvv"
                                        name="cvv"
                                        inputmode="numeric"
                                        autocomplete="cc-csc"
                                        placeholder="123"
                                        class="w-full"
                                        [ngModel]="cvv"
                                        (ngModelChange)="onCvvChange($event)"
                                    />
                                </div>
                                <div class="col-span-12 flex flex-col gap-1">
                                    <small *ngIf="submitted && !expiryValid()" class="text-red-600">Select a valid, non-expired expiry date.</small>
                                    <small *ngIf="submitted && !cvvValid()" class="text-red-600">Enter a {{ cardBrand() === 'AMEX' ? '4' : '3' }}-digit CVV.</small>
                                </div>
                            </div>

                            <div class="flex flex-col gap-2">
                                <label for="billingZip" class="font-medium text-sm">Billing postal code</label>
                                <input pInputText id="billingZip" name="billingZip" [(ngModel)]="billingZip" autocomplete="postal-code" class="w-full" />
                                <small *ngIf="submitted && !zipValid()" class="text-red-600">Enter your billing postal code.</small>
                            </div>

                            <p-message *ngIf="payError" severity="error" [text]="payError"></p-message>

                            <p-button severity="help" type="submit" label="Pay {{ total() | currency: 'USD' }}" icon="pi pi-lock" [loading]="paying" styleClass="w-full"></p-button>

                            <p class="m-0 text-xs text-surface-500 dark:text-surface-400">
                                Demo checkout. Card details are never sent anywhere and are cleared when you leave this page.
                            </p>
                        </form>
                    </div>

                    <div class="col-span-12 lg:col-span-5">
                        <div class="card border border-surface-200 dark:border-surface-700 flex flex-col gap-4">
                            <h4 class="m-0 text-xl font-semibold">Order summary</h4>

                            <div *ngFor="let row of rows" class="flex items-center gap-3">
                                <img
                                    *ngIf="row.product"
                                    class="w-12 h-12 rounded object-cover border border-surface-200 dark:border-surface-700"
                                    [src]="productImage(row.product)"
                                    [alt]="row.product.name || row.item.productId"
                                />
                                <div class="flex flex-col">
                                    <span class="font-medium">{{ row.product?.name || row.item.productId }}</span>
                                    <span class="text-sm text-surface-500 dark:text-surface-400">Qty {{ row.item.quantity }}</span>
                                </div>
                                <span class="ml-auto font-medium">{{ lineSubtotal(row) | currency: 'USD' }}</span>
                            </div>

                            <div class="border-t border-surface-200 dark:border-surface-700 pt-3 flex flex-col gap-2">
                                <div class="flex justify-between text-sm">
                                    <span class="text-surface-500 dark:text-surface-400">Subtotal</span>
                                    <span>{{ subtotal() | currency: 'USD' }}</span>
                                </div>
                                <div class="flex justify-between text-sm">
                                    <span class="text-surface-500 dark:text-surface-400">Shipping</span>
                                    <span>{{ shipping() === 0 ? 'Free' : (shipping() | currency: 'USD') }}</span>
                                </div>
                                <div class="flex justify-between text-sm">
                                    <span class="text-surface-500 dark:text-surface-400">Tax (8%)</span>
                                    <span>{{ tax() | currency: 'USD' }}</span>
                                </div>
                                <div class="flex justify-between text-lg font-semibold border-t border-surface-200 dark:border-surface-700 pt-2">
                                    <span>Total</span>
                                    <span>{{ total() | currency: 'USD' }}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    `
})
export class StoreCheckout implements OnInit {
    private readonly productService = inject(ProductService);
    private readonly http = inject(HttpClient);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly apiBase = 'http://localhost:8080';

    hasToken = false;
    loading = false;
    error = '';
    cart: CartResponse | null = null;
    rows: CheckoutRow[] = [];

    cardName = '';
    cardNumber = '';
    expMonth: string | null = null;
    expYear: string | null = null;
    cvv = '';
    billingZip = '';

    submitted = false;
    paying = false;
    payError = '';
    paid = false;
    reference = '';
    last4 = '';

    readonly months = Array.from({ length: 12 }, (_, i) => String(i + 1).padStart(2, '0'));
    readonly years = Array.from({ length: 12 }, (_, i) => String(new Date().getFullYear() + i));

    ngOnInit() {
        this.hasToken = !!localStorage.getItem(TOKEN_STORAGE_KEY);
        if (this.hasToken) {
            void this.loadCart();
        }
    }

    async loadCart() {
        this.loading = true;
        this.error = '';

        try {
            this.cart = await this.productService.getMyCart();
            this.rows = await Promise.all(
                this.cart.items.map(async (item) => {
                    try {
                        return { item, product: await this.productService.getStoreProductById(item.productId) } satisfies CheckoutRow;
                    } catch {
                        return { item, product: null } satisfies CheckoutRow;
                    }
                })
            );
        } catch (e: any) {
            // The cart plugin deletes the cart after a successful payment, so 404 means empty, not broken.
            this.error = e?.status === 404 ? '' : (e?.error?.error ?? e?.message ?? 'Failed to load cart.');
            this.cart = null;
            this.rows = [];
        } finally {
            this.loading = false;
            this.cdr.detectChanges();
        }
    }

    onCardNumberChange(value: string) {
        const digits = (value ?? '').replace(/\D/g, '').slice(0, 19);
        this.cardNumber = digits.replace(/(.{4})/g, '$1 ').trim();
        this.cdr.detectChanges();
    }

    onCvvChange(value: string) {
        this.cvv = (value ?? '').replace(/\D/g, '').slice(0, 4);
        this.cdr.detectChanges();
    }

    cardDigits() {
        return this.cardNumber.replace(/\D/g, '');
    }

    cardBrand() {
        const digits = this.cardDigits();
        if (/^4/.test(digits)) return 'VISA';
        if (/^(5[1-5]|2[2-7])/.test(digits)) return 'MASTERCARD';
        if (/^3[47]/.test(digits)) return 'AMEX';
        if (/^6(011|5)/.test(digits)) return 'DISCOVER';
        return '';
    }

    cardNameValid() {
        return this.cardName.trim().length >= 2;
    }

    cardNumberValid() {
        const digits = this.cardDigits();
        return digits.length >= 13 && digits.length <= 19 && this.luhnValid(digits);
    }

    expiryValid() {
        if (!this.expMonth || !this.expYear) return false;
        const now = new Date();
        const expiresAt = new Date(Number(this.expYear), Number(this.expMonth), 1);
        return expiresAt > now;
    }

    cvvValid() {
        return this.cvv.length === (this.cardBrand() === 'AMEX' ? 4 : 3);
    }

    zipValid() {
        return this.billingZip.trim().length >= 3;
    }

    formValid() {
        return this.cardNameValid() && this.cardNumberValid() && this.expiryValid() && this.cvvValid() && this.zipValid();
    }

    async pay(form: NgForm) {
        this.submitted = true;
        this.payError = '';

        if (!this.formValid()) {
            this.payError = 'Please correct the highlighted fields.';
            this.cdr.detectChanges();
            return;
        }

        this.paying = true;
        this.cdr.detectChanges();

        try {
            const response = await firstValueFrom(
                this.http.post<PaymentResponse>(
                    `${this.apiBase}/api/p/payment-plugin/payments/me/pay`,
                    {
                        currencyCode: 'usd',
                        paymentMethod: 'card',
                        correlationId: null
                    }
                )
            );

            this.last4 = this.cardDigits().slice(-4);
            this.reference = response.id;
            this.paid = true;
        } catch (e: any) {
            this.payError = e?.error?.error ?? e?.error?.message ?? e?.message ?? 'Payment failed. Please try again.';
        } finally {
            this.paying = false;

            this.cardName = '';
            this.cardNumber = '';
            this.expMonth = null;
            this.expYear = null;
            this.cvv = '';
            this.billingZip = '';
            this.submitted = false;
            form.resetForm();

            this.cdr.detectChanges();
        }
    }

    subtotal() {
        return this.rows.reduce((sum, row) => sum + this.lineSubtotal(row), 0);
    }

    shipping() {
        const subtotal = this.subtotal();
        return subtotal > 0 && subtotal < 99 ? 9.99 : 0;
    }

    tax() {
        return Math.round(this.subtotal() * 0.08 * 100) / 100;
    }

    total() {
        return this.subtotal() + this.shipping() + this.tax();
    }

    lineSubtotal(row: CheckoutRow) {
        return (row.product?.price ?? 0) * (row.item.quantity ?? 0);
    }

    productImage(product: Product) {
        return this.productService.imageUrl(product) ?? `https://primefaces.org/cdn/primeng/images/demo/product/${product.image || 'placeholder.png'}`;
    }

    private luhnValid(digits: string) {
        let sum = 0;
        let double = false;

        for (let i = digits.length - 1; i >= 0; i--) {
            let value = Number(digits[i]);
            if (double) {
                value *= 2;
                if (value > 9) value -= 9;
            }
            sum += value;
            double = !double;
        }

        return sum % 10 === 0;
    }
}
