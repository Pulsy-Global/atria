import { CommonModule } from '@angular/common';
import {
    Component,
    Input,
    OnChanges,
    SimpleChanges,
    ViewEncapsulation,
} from '@angular/core';

import { NetworkDisplayIconSource } from './network-display-icons';

export type NetworkIconSize = 'sm' | 'md' | 'lg';

@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: 'network-icon',
    standalone: true,
    imports: [CommonModule],
    template: `
        <span
            class="network-display-icon text-secondary bg-hover flex shrink-0 items-center justify-center overflow-hidden rounded-full"
            [ngClass]="iconClasses"
            aria-hidden="true"
        >
            <img
                *ngIf="showNetworkIcon; else fallbackInitialIcon"
                class="network-display-svg-icon block h-full w-full"
                [src]="iconSource?.url"
                alt=""
                (error)="handleIconError()"
            />
            <ng-template #fallbackInitialIcon>
                <span
                    class="text-secondary font-semibold uppercase"
                    [ngClass]="fallbackTextClasses"
                >
                    {{ fallbackInitial }}
                </span>
            </ng-template>
        </span>
    `,
    styles: [
        `
            network-icon {
                display: inline-flex;
            }

            .network-display-svg-icon {
                object-fit: contain;
            }
        `,
    ],
    encapsulation: ViewEncapsulation.None,
})
export class NetworkIconComponent implements OnChanges {
    @Input() iconSource: NetworkDisplayIconSource | undefined;
    @Input() fallbackLabel: string | null | undefined;
    @Input() size: NetworkIconSize = 'md';

    showFallbackIcon = false;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['iconSource']) {
            this.showFallbackIcon = false;
        }
    }

    get fallbackInitial(): string {
        return this.fallbackLabel?.charAt(0) || '?';
    }

    get showNetworkIcon(): boolean {
        return !!this.iconSource && !this.showFallbackIcon;
    }

    get iconClasses(): string[] {
        const sizeClasses: Record<NetworkIconSize, string> = {
            sm: 'h-5 w-5',
            md: 'h-7 w-7',
            lg: 'h-10 w-10',
        };

        return [sizeClasses[this.size]];
    }

    get fallbackTextClasses(): string[] {
        const sizeClasses: Record<NetworkIconSize, string> = {
            sm: 'text-[0.65rem]',
            md: 'text-xs',
            lg: 'text-sm',
        };

        return [sizeClasses[this.size]];
    }

    handleIconError(): void {
        this.showFallbackIcon = true;
    }
}
