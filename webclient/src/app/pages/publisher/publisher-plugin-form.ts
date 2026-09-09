import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { MarketplaceApi, PluginManifest } from './marketplace.api';

@Component({
    selector: 'app-publisher-plugin-form',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        InputTextModule,
        InputNumberModule,
        CheckboxModule,
        TextareaModule,
        ToastModule
    ],
    providers: [MessageService],
    template: `
        <p-toast />
        <div class="card">
            <div class="flex items-center justify-between mb-4">
                <div>
                    <div class="font-semibold text-xl">{{ isEdit ? 'Edit plugin' : 'Publish plugin' }}</div>
                    <div class="text-sm text-muted-color">
                        Push the Docker image first. This form publishes the manifest only.
                    </div>
                </div>
                <p-button label="Back to my plugins" icon="pi pi-arrow-left" severity="secondary" routerLink="/publisher/plugins" />
            </div>

            <div class="mb-6 p-4 border rounded-lg surface-border">
                <div class="font-medium mb-2">Paste plugin.json (optional)</div>
                <textarea pTextarea [(ngModel)]="jsonPaste" rows="8" class="w-full mb-3 font-mono text-sm"></textarea>
                <p-button label="Apply JSON to form" icon="pi pi-download" severity="secondary" (onClick)="applyJson()" />
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                    <label class="block font-bold mb-2">Plugin id</label>
                    <input pInputText [(ngModel)]="form.id" class="w-full" [disabled]="isEdit" placeholder="reviews-plugin" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Name</label>
                    <input pInputText [(ngModel)]="form.name" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Version</label>
                    <input pInputText [(ngModel)]="form.version" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Publisher</label>
                    <input pInputText [(ngModel)]="form.publisher" class="w-full" />
                </div>
                <div class="md:col-span-2">
                    <label class="block font-bold mb-2">Description</label>
                    <textarea pTextarea [(ngModel)]="form.description" rows="3" class="w-full"></textarea>
                </div>
                <div class="md:col-span-2">
                    <label class="block font-bold mb-2">Docker image</label>
                    <input pInputText [(ngModel)]="form.image" class="w-full font-mono" placeholder="localhost:5000/ecommerce/reviews-plugin:1.0.0" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Container port</label>
                    <p-inputNumber [(ngModel)]="form.containerPort" [min]="1" [max]="65535" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Health endpoint</label>
                    <input pInputText [(ngModel)]="form.healthEndpoint" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Host API</label>
                    <input pInputText [(ngModel)]="form.hostApi" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Permissions (comma-separated)</label>
                    <input pInputText [(ngModel)]="permissionsText" class="w-full" placeholder="none" />
                </div>
            </div>

            <div class="mt-4 flex items-center gap-2">
                <p-checkbox [(ngModel)]="wantsDatabase" [binary]="true" inputId="db" />
                <label for="db" class="font-medium">Provision a Postgres sidecar</label>
            </div>
            <div *ngIf="wantsDatabase" class="grid grid-cols-1 md:grid-cols-2 gap-4 mt-3 p-4 border rounded-lg surface-border">
                <div>
                    <label class="block font-bold mb-2">DB name</label>
                    <input pInputText [(ngModel)]="database.databaseName" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">DB image</label>
                    <input pInputText [(ngModel)]="database.image" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Username</label>
                    <input pInputText [(ngModel)]="database.username" class="w-full" />
                </div>
                <div>
                    <label class="block font-bold mb-2">Password</label>
                    <input pInputText [(ngModel)]="database.password" class="w-full" />
                </div>
            </div>

            <div class="mt-4 flex items-center gap-2">
                <p-checkbox [(ngModel)]="wantsStorage" [binary]="true" inputId="storage" />
                <label for="storage" class="font-medium">Named volume for files</label>
            </div>
            <div *ngIf="wantsStorage" class="mt-3">
                <label class="block font-bold mb-2">Volume mount path</label>
                <input pInputText [(ngModel)]="storagePath" class="w-full" />
            </div>

            <div class="flex gap-2 mt-6">
                <p-button
                    [label]="isEdit ? 'Save listing' : 'Publish to marketplace'"
                    icon="pi pi-upload"
                    [loading]="saving"
                    (onClick)="save()"
                />
                <p-button label="Cancel" severity="secondary" routerLink="/publisher/plugins" />
            </div>
        </div>
    `
})
export class PublisherPluginForm implements OnInit {
    private readonly api = inject(MarketplaceApi);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly messages = inject(MessageService);

    isEdit = false;
    saving = false;
    jsonPaste = '';
    permissionsText = '';
    wantsDatabase = false;
    wantsStorage = false;
    storagePath = '/app/storage';
    form: PluginManifest = emptyManifest();
    database = emptyDatabase();

    async ngOnInit() {
        const pluginId = this.route.snapshot.paramMap.get('pluginId');
        if (!pluginId) {
            this.cdr.detectChanges();
            return;
        }

        this.isEdit = true;
        try {
            const listing = await this.api.getListing(pluginId);
            this.applyManifest(listing.manifest);
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Load failed', detail: this.api.errorMessage(e) });
        } finally {
            this.cdr.detectChanges();
        }
    }

    applyJson() {
        try {
            const parsed = JSON.parse(this.jsonPaste) as PluginManifest;
            this.applyManifest(parsed);
            this.messages.add({ severity: 'success', summary: 'Applied', detail: parsed.id });
        } catch {
            this.messages.add({ severity: 'error', summary: 'Invalid JSON' });
        }
        this.cdr.detectChanges();
    }

    async save() {
        this.saving = true;
        const manifest = this.toManifest();
        try {
            if (this.isEdit) {
                await this.api.update(manifest.id, manifest);
                this.messages.add({
                    severity: 'success',
                    summary: 'Updated',
                    detail: 'Listing saved. Install from Plugin Catalog if it is not running yet.'
                });
            } else {
                await this.api.publish(manifest);
                this.messages.add({
                    severity: 'success',
                    summary: 'Published',
                    detail: `${manifest.id} is now in the marketplace.`
                });
            }
            await this.router.navigate(['/publisher/plugins']);
        } catch (e) {
            this.messages.add({ severity: 'error', summary: 'Save failed', detail: this.api.errorMessage(e) });
            this.saving = false;
            this.cdr.detectChanges();
        }
    }

    private applyManifest(manifest: PluginManifest) {
        this.form = {
            ...emptyManifest(),
            ...manifest,
            containerPort: manifest.containerPort || 8080,
            healthEndpoint: manifest.healthEndpoint || '/health',
            hostApi: manifest.hostApi || '^1.0.0',
            permissions: manifest.permissions ?? [],
            uiExtensions: manifest.uiExtensions ?? {}
        };
        this.permissionsText = (manifest.permissions ?? []).join(', ');
        this.wantsDatabase = !!manifest.database;
        this.database = { ...emptyDatabase(), ...(manifest.database ?? {}) };
        this.wantsStorage = !!manifest.storage;
        this.storagePath = manifest.storage?.volumeMountPath || '/app/storage';
    }

    private toManifest(): PluginManifest {
        return {
            ...this.form,
            permissions: this.permissionsText
                .split(',')
                .map((p) => p.trim())
                .filter(Boolean),
            database: this.wantsDatabase ? this.database : null,
            storage: this.wantsStorage ? { volumeMountPath: this.storagePath } : null,
            uiExtensions: this.form.uiExtensions ?? {}
        };
    }
}

function emptyManifest(): PluginManifest {
    return {
        id: '',
        name: '',
        version: '1.0.0',
        description: '',
        publisher: '',
        image: '',
        containerPort: 8080,
        healthEndpoint: '/health',
        hostApi: '^1.0.0',
        permissions: [],
        uiExtensions: {}
    };
}

function emptyDatabase() {
    return {
        engine: 'postgres',
        image: 'postgres:16-alpine',
        databaseName: 'plugin',
        username: 'plugin',
        password: 'plugin',
        port: 5432,
        volumeMountPath: '/var/lib/postgresql/data'
    };
}
