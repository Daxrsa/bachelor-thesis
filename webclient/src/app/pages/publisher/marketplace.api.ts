import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, timeout } from 'rxjs';

export interface PluginManifest {
    id: string;
    name: string;
    version: string;
    description: string;
    publisher: string;
    image: string;
    containerPort: number;
    healthEndpoint: string;
    hostApi: string;
    permissions: string[];
    database?: {
        engine: string;
        image: string;
        databaseName: string;
        username: string;
        password: string;
        port: number;
        volumeMountPath: string;
    } | null;
    storage?: {
        volumeMountPath: string;
    } | null;
    uiExtensions: Record<string, string>;
}

export interface MarketplaceListingDto {
    manifest: PluginManifest;
    publishedByUserId: string;
    publishedAt: string;
    updatedAt: string;
    installed: boolean;
    installState: string | null;
}

export interface PublisherRequestDto {
    id: string;
    userId: string;
    email: string;
    message: string;
    status: string;
    createdAt: string;
    reviewedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class MarketplaceApi {
    private readonly http = inject(HttpClient);
    private readonly apiBase = 'http://localhost:8080';
    private readonly timeoutMs = 45000;

    listMine() {
        return this.send(this.http.get<MarketplaceListingDto[]>(`${this.apiBase}/api/marketplace/listings/mine`));
    }

    getListing(pluginId: string) {
        return this.send(this.http.get<MarketplaceListingDto>(`${this.apiBase}/api/marketplace/listings/${pluginId}`));
    }

    publish(manifest: PluginManifest) {
        return this.send(this.http.post(`${this.apiBase}/api/marketplace/listings`, manifest));
    }

    update(pluginId: string, manifest: PluginManifest) {
        return this.send(this.http.put(`${this.apiBase}/api/marketplace/listings/${pluginId}`, manifest));
    }

    unpublish(pluginId: string) {
        return this.send(this.http.delete(`${this.apiBase}/api/marketplace/listings/${pluginId}`));
    }

    myPublisherRequest() {
        return this.send(this.http.get<PublisherRequestDto | null>(`${this.apiBase}/api/marketplace/publisher-requests/mine`));
    }

    requestPublisher(message: string) {
        return this.send(
            this.http.post<PublisherRequestDto>(`${this.apiBase}/api/marketplace/publisher-requests`, { message })
        );
    }

    pendingPublisherRequests() {
        return this.send(this.http.get<PublisherRequestDto[]>(`${this.apiBase}/api/marketplace/publisher-requests`));
    }

    approvePublisherRequest(id: string) {
        return this.send(
            this.http.post<PublisherRequestDto>(`${this.apiBase}/api/marketplace/publisher-requests/${id}/approve`, {})
        );
    }

    rejectPublisherRequest(id: string) {
        return this.send(
            this.http.post<PublisherRequestDto>(`${this.apiBase}/api/marketplace/publisher-requests/${id}/reject`, {})
        );
    }

    errorMessage(e: unknown): string {
        const err = e as { error?: { error?: string }; message?: string };
        return err?.error?.error ?? err?.message ?? 'Request failed';
    }

    private send<T>(obs: Observable<T>) {
        return firstValueFrom(obs.pipe(timeout(this.timeoutMs)));
    }
}
