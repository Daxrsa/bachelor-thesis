import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { TOKEN_STORAGE_KEY } from './auth/token-storage';

function decodeJwtPayload(token: string): Record<string, any> | null {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    try {
        const base64Url = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        const padded = base64Url.padEnd(Math.ceil(base64Url.length / 4) * 4, '=');
        const json = atob(padded);
        return JSON.parse(json);
    } catch {
        return null;
    }
}

function hasValidJwt(token: string): boolean {
    const payload = decodeJwtPayload(token);
    if (!payload) return false;

    if (typeof payload['exp'] !== 'number') return false;
    const nowInSeconds = Math.floor(Date.now() / 1000);
    return payload['exp'] > nowInSeconds;
}

export const authGuard: CanActivateFn = (_route, state) => {
    const router = inject(Router);
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);

    if (token && hasValidJwt(token)) {
        return true;
    }

    localStorage.removeItem(TOKEN_STORAGE_KEY);
    return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl: state.url }
    });
};
