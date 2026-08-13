import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { Product, ProductService } from '@/app/pages/service/product.service';

@Component({
    selector: 'app-products-plugin-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        InputNumberModule,
        TagModule,
        ToastModule,
        ToolbarModule,
        ConfirmDialogModule
    ],
    providers: [ProductService, MessageService, ConfirmationService],
    template: `
        <div class="card">
            <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
                <div>
                    <div class="font-semibold text-xl">Products Plugin Dashboard</div>
                    <div class="text-sm text-muted-color">CRUD actions are proxied through ProductsPluginController.</div>
                </div>
                <div class="flex flex-wrap gap-2">
                    <p-button label="Refresh" icon="pi pi-refresh" severity="secondary" [loading]="loading()" (onClick)="loadProducts()" />
                    <p-button label="Health" icon="pi pi-heart" severity="contrast" [loading]="healthLoading()" (onClick)="checkHealth()" />
                    <p-button label="Greeting" icon="pi pi-comment" severity="info" [loading]="greetingLoading()" (onClick)="loadGreeting()" />
                </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-3 mb-4">
                <div class="p-3 border rounded-lg surface-border">
                    <div class="text-xs text-muted-color mb-1">Plugin Health</div>
                    <div class="font-medium">{{ pluginHealth() || 'not checked yet' }}</div>
                </div>
                <div class="p-3 border rounded-lg surface-border md:col-span-2">
                    <div class="text-xs text-muted-color mb-1">Greeting</div>
                    <div class="font-medium">{{ greeting() || 'not loaded yet' }}</div>
                </div>
            </div>

            <p-toolbar styleClass="mb-4">
                <ng-template #start>
                    <p-button label="New Product" icon="pi pi-plus" severity="success" (onClick)="openNew()" />
                </ng-template>
            </p-toolbar>

            <p-table [value]="products()" [loading]="loading()" [rows]="10" [paginator]="true" dataKey="id" [tableStyle]="{ 'min-width': '70rem' }">
                <ng-template #header>
                    <tr>
                        <th>Image</th>
                        <th>Code</th>
                        <th>Name</th>
                        <th>Price</th>
                        <th>Quantity</th>
                        <th>Status</th>
                        <th>Category</th>
                        <th style="width: 12rem"></th>
                    </tr>
                </ng-template>
                <ng-template #body let-product>
                    <tr>
                        <td>
                            <img
                                *ngIf="productService.imageUrl(product) as url; else noImage"
                                [src]="url"
                                [alt]="product.name || 'product image'"
                                class="w-14 h-14 object-cover rounded border"
                            />
                            <ng-template #noImage>
                                <span class="text-sm text-muted-color">No image</span>
                            </ng-template>
                        </td>
                        <td>{{ product.code }}</td>
                        <td>{{ product.name }}</td>
                        <td>{{ product.price | currency: 'USD' }}</td>
                        <td>{{ product.quantity }}</td>
                        <td><p-tag [value]="product.inventoryStatus" [severity]="severity(product.inventoryStatus)"></p-tag></td>
                        <td>{{ product.category }}</td>
                        <td>
                            <div class="flex gap-2 justify-end">
                                <p-button icon="pi pi-pencil" [rounded]="true" [outlined]="true" (click)="openEdit(product.id)" />
                                <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [outlined]="true" (click)="remove(product)" />
                            </div>
                        </td>
                    </tr>
                </ng-template>
            </p-table>
        </div>

        <p-dialog [(visible)]="dialogVisible" [style]="{ width: '520px' }" [modal]="true" [header]="editingId ? 'Edit Product' : 'Create Product'">
            <ng-template #content>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                    <div class="md:col-span-2">
                        <label for="name" class="block font-bold mb-2">Name</label>
                        <input id="name" type="text" pInputText [(ngModel)]="form.name" class="w-full" />
                    </div>

                    <div>
                        <label for="code" class="block font-bold mb-2">Code</label>
                        <input id="code" type="text" pInputText [(ngModel)]="form.code" class="w-full" />
                    </div>

                    <div>
                        <label for="category" class="block font-bold mb-2">Category</label>
                        <input id="category" type="text" pInputText [(ngModel)]="form.category" class="w-full" />
                    </div>

                    <div class="md:col-span-2">
                        <label for="description" class="block font-bold mb-2">Description</label>
                        <input id="description" type="text" pInputText [(ngModel)]="form.description" class="w-full" />
                    </div>

                    <div>
                        <label for="price" class="block font-bold mb-2">Price</label>
                        <p-inputnumber id="price" mode="currency" currency="USD" locale="en-US" [(ngModel)]="form.price" [min]="0" styleClass="w-full" inputStyleClass="w-full" />
                    </div>

                    <div>
                        <label for="quantity" class="block font-bold mb-2">Quantity</label>
                        <p-inputnumber id="quantity" [(ngModel)]="form.quantity" [min]="0" styleClass="w-full" inputStyleClass="w-full" />
                    </div>

                    <div>
                        <label for="inventoryStatus" class="block font-bold mb-2">Inventory Status</label>
                        <input id="inventoryStatus" type="text" pInputText [(ngModel)]="form.inventoryStatus" class="w-full" placeholder="INSTOCK" />
                    </div>

                    <div>
                        <label for="rating" class="block font-bold mb-2">Rating</label>
                        <p-inputnumber id="rating" [(ngModel)]="form.rating" [min]="0" [max]="5" styleClass="w-full" inputStyleClass="w-full" />
                    </div>

                    <div class="md:col-span-2">
                        <label for="imageFile" class="block font-bold mb-2">Image</label>
                        <input id="imageFile" type="file" accept="image/*" class="w-full" (change)="onImageSelected($event)" />
                        <small class="text-muted-color" *ngIf="form.imageFileName">Current file: {{ form.imageFileName }}</small>
                    </div>
                </div>
            </ng-template>

            <ng-template #footer>
                <p-button label="Cancel" icon="pi pi-times" text (onClick)="closeDialog()" />
                <p-button label="Save" icon="pi pi-check" [loading]="saving()" (onClick)="save()" />
            </ng-template>
        </p-dialog>

        <p-toast></p-toast>
        <p-confirmdialog></p-confirmdialog>
    `
})
export class ProductsPluginDashboard implements OnInit {
    products = signal<Product[]>([]);
    loading = signal(false);
    saving = signal(false);
    healthLoading = signal(false);
    greetingLoading = signal(false);

    pluginHealth = signal('');
    greeting = signal('');

    dialogVisible = false;
    editingId: string | null = null;
    form: Product = this.emptyProduct();
    selectedImageFile: File | null = null;

    constructor(
        readonly productService: ProductService,
        private readonly messageService: MessageService,
        private readonly confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        void this.loadProducts();
    }

    async loadProducts(): Promise<void> {
        this.loading.set(true);
        try {
            const items = await this.productService.getStoreProducts();
            this.products.set(items);
        } catch (error) {
            this.notifyError('Failed to load products', error);
        } finally {
            this.loading.set(false);
        }
    }

    async checkHealth(): Promise<void> {
        this.healthLoading.set(true);
        try {
            const health = await this.productService.getProductsPluginHealth();
            this.pluginHealth.set(`${health.status} (${health.plugin})`);
        } catch (error) {
            this.notifyError('Failed to check plugin health', error);
        } finally {
            this.healthLoading.set(false);
        }
    }

    async loadGreeting(): Promise<void> {
        this.greetingLoading.set(true);
        try {
            const greeting = await this.productService.getProductsPluginGreeting();
            this.greeting.set(greeting.message);
        } catch (error) {
            this.notifyError('Failed to fetch greeting', error);
        } finally {
            this.greetingLoading.set(false);
        }
    }

    openNew(): void {
        this.editingId = null;
        this.form = this.emptyProduct();
        this.selectedImageFile = null;
        this.dialogVisible = true;
    }

    async openEdit(productId?: string): Promise<void> {
        if (!productId) {
            return;
        }

        this.saving.set(true);
        try {
            const product = await this.productService.getStoreProductById(productId);
            this.editingId = product.id ?? productId;
            this.form = { ...product };
            this.selectedImageFile = null;
            this.dialogVisible = true;
        } catch (error) {
            this.notifyError('Failed to load selected product', error);
        } finally {
            this.saving.set(false);
        }
    }

    closeDialog(): void {
        this.dialogVisible = false;
        this.selectedImageFile = null;
    }

    onImageSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedImageFile = input.files?.[0] ?? null;
    }

    async save(): Promise<void> {
        const payload = this.toPayload(this.form);
        if (!payload) {
            this.messageService.add({ severity: 'warn', summary: 'Missing fields', detail: 'Name, price, quantity and inventory status are required.' });
            return;
        }

        this.saving.set(true);
        try {
            if (this.selectedImageFile) {
                const uploaded = await this.productService.uploadImageToFilesPlugin(this.selectedImageFile);
                payload.imageFileName = uploaded.fileName;
                payload.imageUrl = this.productService.imageUrl({ imageFileName: uploaded.fileName }) ?? undefined;
                payload.image = uploaded.fileName;
            }

            if (this.editingId) {
                await this.productService.updateStoreProduct(this.editingId, payload);
                this.messageService.add({ severity: 'success', summary: 'Updated', detail: 'Product updated.' });
            } else {
                await this.productService.createStoreProduct(payload);
                this.messageService.add({ severity: 'success', summary: 'Created', detail: 'Product created.' });
            }

            this.dialogVisible = false;
            this.selectedImageFile = null;
            await this.loadProducts();
        } catch (error) {
            this.notifyError('Failed to save product', error);
        } finally {
            this.saving.set(false);
        }
    }

    remove(product: Product): void {
        this.confirmationService.confirm({
            header: 'Delete Product',
            message: `Delete ${product.name ?? 'this product'}?`,
            icon: 'pi pi-exclamation-triangle',
            accept: async () => {
                if (!product.id) {
                    return;
                }
                try {
                    await this.productService.deleteStoreProduct(product.id);
                    this.messageService.add({ severity: 'success', summary: 'Deleted', detail: 'Product deleted.' });
                    await this.loadProducts();
                } catch (error) {
                    this.notifyError('Failed to delete product', error);
                }
            }
        });
    }

    severity(status?: string): 'success' | 'warn' | 'danger' | 'info' {
        switch ((status ?? '').toUpperCase()) {
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

    private emptyProduct(): Product {
        return {
            code: '',
            name: '',
            description: '',
            price: 0,
            quantity: 0,
            inventoryStatus: 'INSTOCK',
            category: '',
            imageFileName: '',
            imageUrl: '',
            image: '',
            rating: 0
        };
    }

    private toPayload(product: Product): Product | null {
        if (!product.name?.trim()) return null;
        if (typeof product.price !== 'number') return null;
        if (typeof product.quantity !== 'number') return null;
        if (!product.inventoryStatus?.trim()) return null;

        const code = product.code?.trim();
        const description = product.description?.trim();
        const category = product.category?.trim();
        const imageFileName = product.imageFileName?.trim();
        const imageUrl = product.imageUrl?.trim();
        const image = product.image?.trim();

        return {
            code: code ? code : undefined,
            name: product.name.trim(),
            description: description ? description : undefined,
            price: product.price,
            quantity: product.quantity,
            inventoryStatus: product.inventoryStatus.trim().toUpperCase(),
            category: category ? category : undefined,
            imageFileName: imageFileName ? imageFileName : undefined,
            imageUrl: imageUrl ? imageUrl : undefined,
            image: image ? image : undefined,
            rating: typeof product.rating === 'number' ? product.rating : 0
        };
    }

    private notifyError(summary: string, error: unknown): void {
        const detail = this.errorMessage(error);
        this.messageService.add({ severity: 'error', summary, detail });
    }

    private errorMessage(error: unknown): string {
        const err = error as
            | {
                  error?: { error?: string; detail?: string; title?: string; message?: string } | string;
                  message?: string;
                  status?: number;
                  statusText?: string;
              }
            | undefined;

        if (typeof err?.error === 'string' && err.error.trim()) {
            return err.error;
        }

        if (err?.error && typeof err.error === 'object') {
            const serverError = err.error.error?.trim();
            const serverDetail = err.error.detail?.trim();
            const serverTitle = err.error.title?.trim();
            const serverMessage = err.error.message?.trim();

            if (serverError && serverDetail) {
                return `${serverError} ${serverDetail}`;
            }

            if (serverError) return serverError;
            if (serverDetail) return serverDetail;
            if (serverTitle) return serverTitle;
            if (serverMessage) return serverMessage;
        }

        if (err?.message?.trim()) {
            return err.message;
        }

        if (typeof err?.status === 'number') {
            return `Request failed (${err.status}${err.statusText ? ` ${err.statusText}` : ''})`;
        }

        return 'Request failed';
    }
}
