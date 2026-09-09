import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { CurrentUserService } from '@/app/auth/current-user.service';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        @for (item of model; track item.label) {
            @if (!item.separator) {
                <li app-menuitem [item]="item" [root]="true"></li>
            } @else {
                <li class="menu-separator"></li>
            }
        }
    </ul> `,
})
export class AppMenu implements OnInit {
    private readonly currentUser = inject(CurrentUserService);
    private readonly cdr = inject(ChangeDetectorRef);
    model: MenuItem[] = [];

    async ngOnInit() {
        await this.currentUser.refresh();
        this.model = this.buildMenu();
        this.cdr.detectChanges();
    }

    private buildMenu(): MenuItem[] {
        const adminItems: MenuItem[] = [
            { label: 'Plugin Catalog', icon: 'pi pi-fw pi-box', routerLink: ['/plugin-catalog'] },
            { label: 'Publisher Portal', icon: 'pi pi-fw pi-send', routerLink: ['/publisher'] },
            { label: 'Publisher Requests', icon: 'pi pi-fw pi-users', routerLink: ['/publisher-requests'] }
        ];

        if (this.currentUser.isPublisher()) {
            adminItems.push({ label: 'My Plugins', icon: 'pi pi-fw pi-th-large', routerLink: ['/publisher/plugins'] });
        }

        return [
            {
                label: 'Home',
                items: [{ label: 'Main Store', icon: 'pi pi-fw pi-home', routerLink: ['/store'] }]
            },
            {
                label: 'Administration',
                items: adminItems
            }
        ];
    }
}
