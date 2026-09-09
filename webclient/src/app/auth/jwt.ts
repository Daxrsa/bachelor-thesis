import { TOKEN_STORAGE_KEY } from './token-storage';

export interface CurrentUser {
    id: string;
    email: string;
    role: string;
}

export function decodeJwtPayload(token: string): Record<string, unknown> | null {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    try {
        const base64Url = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        const padded = base64Url.padEnd(Math.ceil(base64Url.length / 4) * 4, '=');
        return JSON.parse(atob(padded));
    } catch {
        return null;
    }
}

export function jwtRole(): string | null {
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token) return null;
    const payload = decodeJwtPayload(token);
    const role =
        payload?.['role'] ??
        payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    return typeof role === 'string' ? role.toLowerCase() : null;
}

export function isPublisherRole(role: string | null | undefined): boolean {
    const value = role?.toLowerCase();
    return value === 'publisher' || value === 'admin';
}
