import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { inject } from '@angular/core';
import { TOKEN_STORAGE_KEY } from './auth/token-storage';
import { CurrentUserService } from './auth/current-user.service';
import { decodeJwtPayload, isPublisherRole, jwtRole } from './auth/jwt';

function hasValidJwt(token: string): boolean {
    const payload = decodeJwtPayload(token);
    if (!payload) return false;

    if (typeof payload['exp'] !== 'number') return false;
    const nowInSeconds = Math.floor(Date.now() / 1000);
    return payload['exp'] > nowInSeconds;
}

function requireAuth(returnUrl: string): true | UrlTree {
    const router = inject(Router);
    const token = localStorage.getItem(TOKEN_STORAGE_KEY);

    if (token && hasValidJwt(token)) {
        return true;
    }

    localStorage.removeItem(TOKEN_STORAGE_KEY);
    return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl }
    });
}

export const authGuard: CanActivateFn = (_route, state) => requireAuth(state.url);

export const publisherGuard: CanActivateFn = async (_route, state) => {
    const router = inject(Router);
    const currentUser = inject(CurrentUserService);
    const auth = requireAuth(state.url);
    if (auth !== true) return auth;

    const role = currentUser.user()?.role ?? jwtRole();
    if (isPublisherRole(role)) return true;

    const me = await currentUser.refresh();
    if (isPublisherRole(me?.role)) return true;
    return router.createUrlTree(['/publisher']);
};

export const adminGuard: CanActivateFn = async (_route, state) => {
    const router = inject(Router);
    const currentUser = inject(CurrentUserService);
    const auth = requireAuth(state.url);
    if (auth !== true) return auth;

    if (currentUser.isAdmin() || jwtRole() === 'admin') return true;

    const me = await currentUser.refresh();
    if (me?.role?.toLowerCase() === 'admin') return true;
    return router.createUrlTree(['/store']);
};

