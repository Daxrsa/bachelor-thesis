import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
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
                <p-button *ngIf="!installed" label="Install Plugin" (onClick)="installPlugin()" [loading]="busy" severity="success" />
                <p-button *ngIf="installed" label="Uninstall Plugin" (onClick)="uninstallPlugin()" [loading]="busy" severity="danger" />
                <p-button label="Call Greeting" (onClick)="callGreeting()" [loading]="busy" severity="contrast" />
            </div>

            <div class="mb-2"><b>Status:</b> {{ status }}</div>
            <div class="mb-2"><b>Installed:</b> {{ installed ? 'Yes' : 'No' }}</div>
            <div class="mb-2"><b>Greeting:</b> {{ greeting }}</div>
            <div class="text-red-500" *ngIf="error"><b>Error:</b> {{ error }}</div>
        </div>
    `
})
export class HelloPluginWidget {
    private readonly http = inject(HttpClient);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly apiBase = 'http://localhost:8080';
    private readonly requestTimeoutMs = 45000;
    private readonly pluginId = 'hello-plugin';
    private readonly tokenStorageKey = 'ecommerce.token';

    email = 'pluginadmin@example.com';
    password = 'Passw0rd!';
    token = '';
    status = 'Idle';
    greeting = '-';
    error = '';
    busy = false;
    installed = false;

    ngOnInit() {
        const storedToken = localStorage.getItem(this.tokenStorageKey);
        if (!storedToken) return;

        this.token = storedToken;
        void this.run(async () => {
            await this.refreshInstalledState();
            this.status = 'Session restored';
        });
    }

    async register() {
        await this.authCall('register');
    }

    async login() {
        await this.authCall('login');
    }

    async installPlugin() {
        await this.run(async () => {
            await this.ensureToken();
            this.status = 'Installing plugin...';

            let install: any;

            try {
                install = await this.send(
                    this.http.post<any>(
                        `${this.apiBase}/api/plugins/${this.pluginId}/install`,
                        { grantedPermissions: [] },
                        { headers: this.authHeaders() }
                    )
                );
            } catch (e: any) {
                const msg = this.errorMessage(e);

                if (this.isTransientInstallError(msg)) {
                    this.status = 'Install retrying after transient startup error...';
                    await this.sleep(2000);
                    install = await this.send(
                        this.http.post<any>(
                            `${this.apiBase}/api/plugins/${this.pluginId}/install`,
                            { grantedPermissions: [] },
                            { headers: this.authHeaders() }
                        )
                    );
                } else if (msg.toLowerCase().includes('already installed')) {
                    this.installed = true;
                    this.status = 'Already installed';
                    this.error = '';
                    return;
                } else {
                    throw e;
                }
            }

            this.status = `Installed (${install.state})`;
            this.installed = true;
            this.error = '';
        });
    }

    async uninstallPlugin() {
        await this.run(async () => {
            await this.ensureToken();
            this.status = 'Uninstalling hello-plugin...';
            await this.send(
                this.http.delete(`${this.apiBase}/api/plugins/${this.pluginId}`, {
                    headers: this.authHeaders()
                })
            );
            this.installed = false;
            this.status = 'Uninstalled';
            this.error = '';
        });
    }

    async callGreeting() {
        await this.run(async () => {
            await this.ensureToken();
            const res = await this.send(
                this.http.get<{ message: string }>(`${this.apiBase}/api/p/${this.pluginId}/greeting`, {
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
            localStorage.setItem(this.tokenStorageKey, this.token);
            await this.refreshInstalledState();
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

    private async refreshInstalledState() {
        const list = await this.send(
            this.http.get<Array<{ pluginId: string }>>(`${this.apiBase}/api/plugins`, {
                headers: this.authHeaders()
            })
        );
        this.installed = list.some((p) => p.pluginId === this.pluginId);
    }

    private errorMessage(e: any) {
        return e?.error?.error ?? e?.message ?? 'Request failed';
    }

    private isTransientInstallError(msg: string) {
        const m = msg.toLowerCase();
        return m.includes('not healthy yet') || m.includes('connection reset by peer') || m.includes('timed out');
    }

    private sleep(ms: number) {
        return new Promise<void>((resolve) => setTimeout(resolve, ms));
    }

    private async run(work: () => Promise<void>) {
        this.busy = true;
        this.error = '';
        try {
            await work();
        } catch (e: any) {
            const msg = this.errorMessage(e);
            if (msg.toLowerCase().includes('unauthorized') || msg.toLowerCase().includes('jwt') || msg.toLowerCase().includes('401')) {
                this.token = '';
                localStorage.removeItem(this.tokenStorageKey);
            }
            this.error = msg;
            this.status = 'Failed';
        } finally {
            this.busy = false;
            // With zoneless change detection, async promise completion may not auto-refresh.
            this.cdr.detectChanges();
        }
    }
}
