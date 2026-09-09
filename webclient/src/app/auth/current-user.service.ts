import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CurrentUser, isPublisherRole, jwtRole } from './jwt';

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
    private readonly http = inject(HttpClient);
    private readonly apiBase = 'http://localhost:8080';

    readonly user = signal<CurrentUser | null>(null);

    async refresh(): Promise<CurrentUser | null> {
        try {
            const me = await firstValueFrom(this.http.get<CurrentUser>(`${this.apiBase}/api/auth/me`));
            this.user.set(me);
            return me;
        } catch {
            this.user.set(null);
            return null;
        }
    }

    role(): string {
        return this.user()?.role?.toLowerCase() ?? jwtRole() ?? 'user';
    }

    isPublisher(): boolean {
        return isPublisherRole(this.role());
    }

    isAdmin(): boolean {
        return this.role() === 'admin';
    }
}
