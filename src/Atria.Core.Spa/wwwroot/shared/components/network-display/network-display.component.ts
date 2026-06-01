import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

import {
    getNetworkDisplayInfo,
    NetworkDisplayInfo,
    NetworkDisplayNetwork,
} from './network-display.helper';
import { NetworkIconComponent } from './network-icon.component';

export type NetworkDisplayVariant = 'stacked' | 'inline' | 'compact';
export type NetworkDisplaySize = 'sm' | 'md' | 'lg';

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: 'network-display',
    standalone: true,
    imports: [CommonModule, NetworkIconComponent],
    template: `
        <div
            class="network-display flex min-w-0 items-center"
            [ngClass]="containerClasses"
        >
            <network-icon
                *ngIf="showIcon"
                [iconSource]="displayInfo.iconSource"
                [fallbackLabel]="displayInfo.networkTitle"
                [size]="size"
            >
            </network-icon>

            <span
                class="network-display-text min-w-0"
                [ngClass]="textContainerClasses"
            >
                <span
                    *ngIf="showNetwork"
                    class="text-default block truncate font-medium"
                    [ngClass]="networkTextClasses"
                >
                    {{ displayInfo.networkTitle }}
                </span>
                <span
                    *ngIf="
                        variant === 'inline' && showNetwork && showEnvironment
                    "
                    class="text-secondary shrink-0"
                    [ngClass]="environmentTextClasses"
                >
                    -
                </span>
                <span
                    *ngIf="showEnvironment"
                    class="text-secondary block truncate"
                    [ngClass]="environmentTextClasses"
                >
                    {{ displayInfo.environmentTitle }}
                </span>
            </span>
        </div>
    `,
})
export class NetworkDisplayComponent implements OnChanges {
    @Input() networks: readonly NetworkDisplayNetwork[] | null | undefined;
    @Input() networkId: string | null | undefined;
    @Input() variant: NetworkDisplayVariant = 'stacked';
    @Input() size: NetworkDisplaySize = 'md';
    @Input() showIcon = true;
    @Input() showNetwork = true;
    @Input() showEnvironment = true;

    displayInfo: NetworkDisplayInfo = getNetworkDisplayInfo(null, null);

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['networks'] || changes['networkId']) {
            this.displayInfo = getNetworkDisplayInfo(
                this.networks,
                this.networkId
            );
        }
    }

    get containerClasses(): string[] {
        const classes = [this.variant === 'stacked' ? 'gap-2' : 'gap-1.5'];

        if (this.variant === 'compact') {
            classes.push('max-w-full');
        }

        return classes;
    }

    get textContainerClasses(): string[] {
        if (this.variant === 'inline') {
            return ['flex', 'min-w-0', 'items-baseline', 'gap-1'];
        }

        return ['block'];
    }

    get networkTextClasses(): string[] {
        const sizeClasses: Record<NetworkDisplaySize, string> = {
            sm: 'text-xs',
            md: 'text-sm',
            lg: 'text-base',
        };

        return [sizeClasses[this.size]];
    }

    get environmentTextClasses(): string[] {
        const sizeClasses: Record<NetworkDisplaySize, string> = {
            sm: 'text-xs',
            md: 'text-xs',
            lg: 'text-sm',
        };

        const classes = [sizeClasses[this.size]];

        return classes;
    }
}
