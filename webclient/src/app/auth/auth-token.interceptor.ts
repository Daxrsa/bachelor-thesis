import { HttpInterceptorFn } from '@angular/common/http';
import { TOKEN_STORAGE_KEY } from './token-storage';

const API_BASE_URL = 'http://localhost:8080';

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
    if (!req.url.startsWith(API_BASE_URL)) {
        return next(req);
    }

    const token = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token) {
        return next(req);
    }

    return next(
        req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        })
    );
};
