import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom, tap } from 'rxjs';
import { TOKEN_STORAGE_KEY } from '@/app/auth/token-storage';

/** Shared across ProductService instances, which are provided per component. */
export const cartItemCount = signal(0);

export interface Product {
    id?: string;
    code?: string;
    name?: string;
    description?: string;
    price?: number;
    quantity?: number;
    inventoryStatus?: string;
    category?: string;
    imageFileName?: string;
    imageUrl?: string;
    image?: string;
    rating?: number;
}

export interface UploadedImage {
    fileName: string;
    contentType: string;
    size: number;
    publicUrl: string;
    uploadedAtUtc: string;
}

export interface AddProductToCartPayload {
    productId: string;
    quantity: number;
    cartId?: string | null;
    cartItemId?: string | null;
    correlationId?: string | null;
}

export interface CartItemResponse {
    id: string;
    productId: string;
    quantity: number;
    createdAtUtc: string;
    updatedAtUtc: string;
}

export interface CartResponse {
    id: string;
    userId: string;
    createdAtUtc: string;
    updatedAtUtc: string;
    items: CartItemResponse[];
}

export interface ProductsPluginHealthResponse {
    status: string;
    plugin: string;
}

export interface ProductsPluginGreetingResponse {
    message: string;
    from: string;
}

export interface PaymentResponse {
    id: string;
    userId: string;
    correlationId: string;
    amount: number;
    currencyCode: string;
    status: string;
    paymentMethod: string;
    providerReference?: string | null;
    failureReason?: string | null;
    createdAtUtc: string;
}

export interface OrderItemResponse {
    productId: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface OrderResponse {
    id: string;
    userId: string;
    cartId: string;
    paymentId: string;
    status: string;
    totalAmount: number;
    currencyCode: string;
    paymentMethod: string;
    providerReference: string;
    paidAtUtc: string;
    createdAtUtc: string;
    items: OrderItemResponse[];
}

export interface StripeWebhookResponse {
    eventId: string;
    eventType: string;
    providerReference?: string | null;
    customerId?: string | null;
    customerEmail?: string | null;
    amount?: number | null;
    currencyCode?: string | null;
    status?: string | null;
    stripeCreatedAtUtc: string;
    receivedAtUtc: string;
}

@Injectable()
export class ProductService {
    private readonly apiBase = 'http://localhost:8080';

    constructor(private http: HttpClient) {}

    getStoreProducts(filters?: { minPrice?: number; maxPrice?: number; availability?: string[] }) {
        const query = new URLSearchParams();
        if (typeof filters?.minPrice === 'number') query.set('minPrice', String(filters.minPrice));
        if (typeof filters?.maxPrice === 'number') query.set('maxPrice', String(filters.maxPrice));
        for (const status of filters?.availability ?? []) {
            query.append('availability', status);
        }

        const suffix = query.toString() ? `?${query.toString()}` : '';
        return firstValueFrom(
            this.http.get<Product[]>(`${this.apiBase}/api/p/products-plugin/products${suffix}`).pipe(
                tap((products) => console.log('[ProductService] getStoreProducts:', products))
            )
        );
    }

    getStoreProductById(id: string) {
        return firstValueFrom(this.http.get<Product>(`${this.apiBase}/api/p/products-plugin/products/${id}`));
    }

    createStoreProduct(payload: Product) {
        return firstValueFrom(this.http.post<Product>(`${this.apiBase}/api/p/products-plugin/products`, payload));
    }

    updateStoreProduct(id: string, payload: Product) {
        return firstValueFrom(this.http.put<Product>(`${this.apiBase}/api/p/products-plugin/products/${encodeURIComponent(id)}`, payload));
    }

    uploadImageToFilesPlugin(file: File) {
        const formData = new FormData();
        formData.append('file', file, file.name);
        return firstValueFrom(this.http.post<UploadedImage>(`${this.apiBase}/api/p/files-plugin/files/images`, formData));
    }

    buildFilesPluginImageUrl(fileName: string) {
        return `${this.apiBase}/api/p/files-plugin/static/images/${encodeURIComponent(fileName)}`;
    }

    imageUrl(product: Product): string | null {
        const fromFilePlugin = product.imageUrl?.trim();
        if (fromFilePlugin) {
            return fromFilePlugin;
        }

        const raw = product.imageFileName?.trim() || product.image?.trim();
        if (!raw) {
            return null;
        }

        if (/^https?:\/\//i.test(raw)) {
            return raw;
        }

        return this.buildFilesPluginImageUrl(raw);
    }

    deleteStoreProduct(id: string) {
        return firstValueFrom(this.http.delete<void>(`${this.apiBase}/api/p/products-plugin/products/${encodeURIComponent(id)}`));
    }

    getProductsPluginHealth() {
        return firstValueFrom(this.http.get<ProductsPluginHealthResponse>(`${this.apiBase}/api/p/products-plugin/health`));
    }

    getProductsPluginGreeting() {
        return firstValueFrom(this.http.get<ProductsPluginGreetingResponse>(`${this.apiBase}/api/p/products-plugin/products/greeting`));
    }

    async addProductToMyCart(payload: AddProductToCartPayload) {
        const result = await firstValueFrom(this.http.post(`${this.apiBase}/api/p/cart-plugin/carts/me/items`, payload));
        void this.refreshCartItemCount(1200);
        return result;
    }

    getMyCart() {
        return firstValueFrom(this.http.get<CartResponse>(`${this.apiBase}/api/p/cart-plugin/carts/me`));
    }

    async removeProductFromMyCart(productId: string, quantity = 1, cartId?: string | null, correlationId?: string | null) {
        const query = new URLSearchParams({ quantity: String(quantity) });
        if (cartId) query.set('cartId', cartId);
        if (correlationId) query.set('correlationId', correlationId);

        const result = await firstValueFrom(this.http.delete(`${this.apiBase}/api/p/cart-plugin/carts/me/items/${encodeURIComponent(productId)}?${query.toString()}`));
        void this.refreshCartItemCount(1200);
        return result;
    }

    getMyCartItemCount() {
        return firstValueFrom(this.http.get<{ count?: number }>(`${this.apiBase}/api/p/cart-plugin/carts/me/items/count`));
    }

    getMyPayments() {
        return firstValueFrom(
            this.http.get<PaymentResponse[]>(`${this.apiBase}/api/p/payment-plugin/payments/me`).pipe(
                tap((payments) => console.log('[ProductService] getMyPayments:', payments))
            )
        );
    }

    getMyOrders() {
        return firstValueFrom(this.http.get<OrderResponse[]>(`${this.apiBase}/api/p/order-plugin/orders/me`));
    }

    getStripeWebhookEvents() {
        return firstValueFrom(
            this.http.get<StripeWebhookResponse[]>(`${this.apiBase}/api/p/payment-plugin/payments/webhook-events`).pipe(
                tap((events) => console.log('[ProductService] getStripeWebhookEvents:', events))
            )
        );
    }

    /** Cart mutations are queued as events, so a delay lets the consumer apply them before re-reading. */
    async refreshCartItemCount(delayMs = 0) {
        if (!localStorage.getItem(TOKEN_STORAGE_KEY)) {
            cartItemCount.set(0);
            return;
        }

        if (delayMs > 0) {
            await new Promise((resolve) => setTimeout(resolve, delayMs));
        }

        try {
            const result = await this.getMyCartItemCount();
            cartItemCount.set(result?.count ?? 0);
        } catch {
            // Cart plugin may not be installed or the user may have no cart.
            cartItemCount.set(0);
        }
    }
}
