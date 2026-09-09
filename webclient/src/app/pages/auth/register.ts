import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { AppFloatingConfigurator } from '../../layout/component/app.floatingconfigurator';
import { TOKEN_STORAGE_KEY } from '../../auth/token-storage';
import { CurrentUserService } from '../../auth/current-user.service';

@Component({
    selector: 'app-register',
    standalone: true,
    imports: [CommonModule, ButtonModule, InputTextModule, PasswordModule, FormsModule, RouterModule, AppFloatingConfigurator],
    template: `
        <app-floating-configurator />
        <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-screen overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, var(--primary-color) 10%, rgba(33, 150, 243, 0) 30%)">
                    <div class="w-full bg-surface-0 dark:bg-surface-900 py-20 px-8 sm:px-20" style="border-radius: 53px">
                        <div class="text-center mb-8">
                            <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-2">Create an account</div>
                            <span class="text-muted-color font-medium">Register to shop or publish plugins</span>
                        </div>

                        <label for="registerEmail" class="block text-surface-900 dark:text-surface-0 text-xl font-medium mb-2">Email</label>
                        <input pInputText id="registerEmail" type="email" placeholder="Email address" class="w-full md:w-120 mb-6" [(ngModel)]="email" />

                        <label for="registerPassword" class="block text-surface-900 dark:text-surface-0 font-medium text-xl mb-2">Password</label>
                        <p-password id="registerPassword" [(ngModel)]="password" placeholder="Password" [toggleMask]="true" styleClass="mb-4" [fluid]="true" [feedback]="true"></p-password>

                        <label for="registerConfirm" class="block text-surface-900 dark:text-surface-0 font-medium text-xl mb-2">Confirm Password</label>
                        <p-password
                            id="registerConfirm"
                            [(ngModel)]="confirmPassword"
                            placeholder="Confirm password"
                            [toggleMask]="true"
                            styleClass="mb-6"
                            [fluid]="true"
                            [feedback]="false"
                        ></p-password>

                        <div class="flex flex-col gap-3">
                            <p-button label="Create Account" styleClass="w-full" [loading]="isSubmitting" [disabled]="!canSubmit()" (onClick)="submit()"></p-button>
                            <p-button label="Already have an account? Sign in" [text]="true" styleClass="w-full" routerLink="/auth/login" />
                        </div>

                        <small class="text-red-600 block mt-4" *ngIf="errorMessage">{{ errorMessage }}</small>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class Register {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly currentUser = inject(CurrentUserService);
    private readonly apiBase = 'http://localhost:8080';

    email = '';
    password = '';
    confirmPassword = '';
    isSubmitting = false;
    errorMessage = '';

    canSubmit(): boolean {
        return !this.isSubmitting && !!this.email.trim() && !!this.password && this.password === this.confirmPassword;
    }

    async submit(): Promise<void> {
        if (!this.canSubmit()) return;

        this.isSubmitting = true;
        this.errorMessage = '';

        try {
            const response = await firstValueFrom(
                this.http.post<{ token: string }>(`${this.apiBase}/api/auth/register`, {
                    email: this.email.trim(),
                    password: this.password
                })
            );

            localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
            await this.currentUser.refresh();
            await this.router.navigateByUrl('/auth/welcome');
        } catch (error: unknown) {
            this.errorMessage = this.buildErrorMessage(error);
        } finally {
            this.isSubmitting = false;
        }
    }

    private buildErrorMessage(error: unknown): string {
        if (error instanceof HttpErrorResponse) {
            const apiMessage = typeof error.error?.error === 'string' ? error.error.error : '';
            if (apiMessage) return apiMessage;
            if (error.status === 0) return 'Cannot reach the API. Check if backend is running on http://localhost:8080.';
            if (error.status === 400) return 'Registration failed. That email may already be registered.';
        }

        return 'Registration failed. Please try again.';
    }
}
