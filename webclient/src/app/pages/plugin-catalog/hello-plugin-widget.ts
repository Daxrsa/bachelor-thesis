import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, Observable, timeout } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';

@Component({
    selector: 'app-hello-plugin-widget',
    standalone: true,
    imports: [CommonModule, FormsModule, InputTextModule, PasswordModule, ButtonModule],
    template: `
        <div class="card">
            <div class="font-semibold text-xl mb-3">Hello Plugin</div>
            <p class="mb-4">Install and test the backend hello-plugin from webclient.</p>

            <div class="mb-3">
                <label class="block mb-2">Email</label>
                <input pInputText [(ngModel)]="email" class="w-full" />
            </div>

            <div class="mb-4">
                <label class="block mb-2">Password</label>
                <p-password [(ngModel)]="password" [toggleMask]="true" [feedback]="false" styleClass="w-full" [fluid]="true" />
            </div>

            <div class="flex flex-wrap gap-2 mb-4">
                <p-button label="Register" (onClick)="register()" [loading]="busy" />
                <p-button label="Login" (onClick)="login()" [loading]="busy" severity="secondary" />
                <p-button label="Install Hello Plugin" (onClick)="installHelloPlugin()" [loading]="busy" severity="success" />
                <p-button label="Call Greeting" (onClick)="callGreeting()" [loading]="busy" severity="contrast" />
            </div>

            <div class="mb-2"><b>Status:</b> {{ status }}</div>
            <div class="mb-2"><b>Greeting:</b> {{ greeting }}</div>
            <div class="text-red-500" *ngIf="error"><b>Error:</b> {{ error }}</div>
        </div>
    `
})
export class HelloPluginWidget {
    private readonly http = inject(HttpClient);
    private readonly apiBase = 'http://localhost:8080';
    private readonly requestTimeoutMs = 45000;

    email = 'pluginadmin@example.com';
    password = 'Passw0rd!';
    token = '';
    status = 'Idle';
    greeting = '-';
    error = '';
    busy = false;

    async register() {
        await this.authCall('register');
    }

    async login() {
        await this.authCall('login');
    }

    async installHelloPlugin() {
        await this.run(async () => {
            await this.ensureToken();
            this.status = 'Installing hello-plugin...';
            await this.send(
                this.http.delete(`${this.apiBase}/api/plugins/hello-plugin`, {
                    headers: this.authHeaders()
                })
            );

            const install = await this.send(
                this.http.post<any>(
                    `${this.apiBase}/api/plugins/hello-plugin/install`,
                    { grantedPermissions: [] },
                    { headers: this.authHeaders() }
                )
            );

            this.status = `Installed (${install.state})`;
            this.error = '';
        });
    }

    async callGreeting() {
        await this.run(async () => {
            await this.ensureToken();
            const res = await this.send(
                this.http.get<{ message: string }>(`${this.apiBase}/api/p/hello-plugin/greeting`, {
                    headers: this.authHeaders()
                })
            );
            this.greeting = res.message;
            this.status = 'Greeting received';
            this.error = '';
        });
    }

    private async authCall(kind: 'register' | 'login') {
        await this.run(async () => {
            const res = await this.send(
                this.http.post<{ token: string }>(`${this.apiBase}/api/auth/${kind}`, {
                    email: this.email,
                    password: this.password
                })
            );

            this.token = res.token;
            this.status = kind === 'register' ? 'Registered and logged in' : 'Logged in';
            this.error = '';
        });
    }

    private async ensureToken() {
        if (!this.token) {
            await this.authCall('login');
        }
    }

    private authHeaders() {
        return new HttpHeaders({ Authorization: `Bearer ${this.token}` });
    }

    private send<T>(obs: Observable<T>) {
        return firstValueFrom(obs.pipe(timeout(this.requestTimeoutMs)));
    }

    private async run(work: () => Promise<void>) {
        this.busy = true;
        this.error = '';
        try {
            await work();
        } catch (e: any) {
            const msg = e?.error?.error ?? e?.message ?? 'Request failed';
            this.error = msg;
            this.status = 'Failed';
        } finally {
            this.busy = false;
        }
    }
}
