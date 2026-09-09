import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AppFloatingConfigurator } from '../../layout/component/app.floatingconfigurator';
import { TOKEN_STORAGE_KEY } from '../../auth/token-storage';
import { jwtRole } from '../../auth/jwt';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [CommonModule, ButtonModule, CheckboxModule, InputTextModule, PasswordModule, FormsModule, RouterModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, var(--primary-color) 10%, rgba(33, 150, 243, 0) 30%)">
                    <div class="w-full bg-surface-0 dark:bg-surface-900 py-20 px-8 sm:px-20" style="border-radius: 53px">
                        <div class="text-center mb-8">
                            <span class="text-muted-color font-medium">Sign in to continue</span>
                        </div>

                        <div>
                            <label for="email1" class="block text-surface-900 dark:text-surface-0 text-xl font-medium mb-2">Email</label>
                            <input pInputText id="email1" type="text" placeholder="Email address" class="w-full md:w-120 mb-8" [(ngModel)]="email" />

                            <label for="password1" class="block text-surface-900 dark:text-surface-0 font-medium text-xl mb-2">Password</label>
                            <p-password id="password1" [(ngModel)]="password" placeholder="Password" [toggleMask]="true" styleClass="mb-4" [fluid]="true" [feedback]="false"></p-password>

                            <div class="flex items-center justify-between mt-2 mb-8 gap-8">
                                <div class="flex items-center">
                                    <p-checkbox [(ngModel)]="checked" id="rememberme1" binary class="mr-2"></p-checkbox>
                                    <label for="rememberme1">Remember me</label>
                                </div>
                                <span class="font-medium no-underline ml-2 text-right cursor-pointer text-primary">Forgot password?</span>
                            </div>
                            <div class="flex flex-col gap-3">
                                <p-button
                                    label="Sign In"
                                    styleClass="w-full"
                                    [loading]="isSubmitting"
                                    [disabled]="!canSubmit()"
                                    (onClick)="submit()"
                                ></p-button>
                                <p-button label="Register here" [text]="true" styleClass="w-full" routerLink="/auth/register" />
                            </div>

                            <small class="text-green-600 block mt-4" *ngIf="statusMessage">{{ statusMessage }}</small>
                            <small class="text-red-600 block mt-2" *ngIf="errorMessage">{{ errorMessage }}</small>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class Login {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly apiBase = 'http://localhost:8080';

    logoUrl = '/demo/images/logo.webp';

    email: string = '';

    password: string = '';

    checked: boolean = false;

    isSubmitting: boolean = false;

    statusMessage: string = '';

    errorMessage: string = '';

    canSubmit(): boolean {
        return !this.isSubmitting && !!this.email && !!this.password;
    }

    async submit(): Promise<void> {
        if (!this.canSubmit()) {
            return;
        }

        this.isSubmitting = true;
        this.statusMessage = '';
        this.errorMessage = '';

        try {
            const response: { token: string } = await firstValueFrom(
                this.http.post<{ token: string }>(`${this.apiBase}/api/auth/login`, {
                    email: this.email,
                    password: this.password
                })
            );

            localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
            this.statusMessage = 'Signed in successfully.';
            await this.router.navigateByUrl(this.destinationAfterLogin());
        } catch (error: unknown) {
            this.errorMessage = this.buildErrorMessage(error);
        } finally {
            this.isSubmitting = false;
        }
    }

    private destinationAfterLogin(): string {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        if (jwtRole() === 'admin') {
            return returnUrl || '/';
        }

        if (returnUrl && (returnUrl.startsWith('/store') || returnUrl.startsWith('/publisher'))) {
            return returnUrl;
        }

        return '/store';
    }

    private buildErrorMessage(error: unknown): string {
        if (error instanceof HttpErrorResponse) {
            const apiMessage = typeof error.error?.error === 'string' ? error.error.error : '';
            if (apiMessage) return apiMessage;
            if (error.status === 0) return 'Cannot reach the API. Check if backend is running on http://localhost:8080.';
            if (error.status === 401) return 'Invalid credentials.';
        }

        return 'Authentication request failed. Please try again.';
    }
}
