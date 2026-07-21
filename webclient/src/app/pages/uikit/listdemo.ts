import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { OrderListModule } from 'primeng/orderlist';
import { PickListModule } from 'primeng/picklist';

@Component({
    selector: 'app-list-demo',
    standalone: true,
    imports: [CommonModule, PickListModule, OrderListModule],
    template: ` <div class="flex flex-col">
        <div class="flex flex-col lg:flex-row gap-20">
            <div class="lg:w-2/3">
                <div class="card">
                    <div class="font-semibold text-xl mb-4">PickList</div>
                    <p-pick-list [source]="sourceCities" [target]="targetCities" breakpoint="1400px">
                        <ng-template #item let-item>
                            {{ item.name }}
                        </ng-template>
                    </p-pick-list>
                </div>
            </div>

            <div class="lg:w-1/3">
                <div class="card">
                    <div class="font-semibold text-xl mb-4">OrderList</div>
                    <p-orderlist [value]="orderCities" dataKey="id" breakpoint="575px">
                        <ng-template #option let-option>
                            {{ option.name }}
                        </ng-template>
                    </p-orderlist>
                </div>
            </div>
        </div>
    </div>`,
    styles: `
        ::ng-deep {
            .p-orderlist-list-container {
                width: 100%;
            }
        }
    `
})
export class ListDemo {
    sourceCities: any[] = [];

    targetCities: any[] = [];

    orderCities: any[] = [];

    ngOnInit() {
        this.sourceCities = [
            { name: 'San Francisco', code: 'SF' },
            { name: 'London', code: 'LDN' },
            { name: 'Paris', code: 'PRS' },
            { name: 'Istanbul', code: 'IST' },
            { name: 'Berlin', code: 'BRL' },
            { name: 'Barcelona', code: 'BRC' },
            { name: 'Rome', code: 'RM' }
        ];

        this.targetCities = [];

        this.orderCities = [
            { name: 'San Francisco', code: 'SF' },
            { name: 'London', code: 'LDN' },
            { name: 'Paris', code: 'PRS' },
            { name: 'Istanbul', code: 'IST' },
            { name: 'Berlin', code: 'BRL' },
            { name: 'Barcelona', code: 'BRC' },
            { name: 'Rome', code: 'RM' }
        ];
    }
}
