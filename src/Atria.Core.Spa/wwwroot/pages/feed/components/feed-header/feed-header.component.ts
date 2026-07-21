import { CommonModule } from '@angular/common';
import {
    Component,
    EventEmitter,
    Input,
    Output,
    ViewEncapsulation,
    inject,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FEED_HEADER_ACTION_COMPONENT } from '../../../../shared/services/feed-header-action.token';

@Component({
    selector: 'feed-header',
    standalone: true,
    templateUrl: './feed-header.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [CommonModule, MatIconModule, MatButtonModule],
})
export class FeedHeaderComponent {
    readonly actionComponent = inject(FEED_HEADER_ACTION_COMPONENT, {
        optional: true,
    });

    @Input() title: string = '';
    @Input() description: string = '';
    @Input() statusLabel: string = '';
    @Input() statusClass: string = '';
    @Input() showBackButton: boolean = true;
    @Input() actionInputs: Record<string, unknown> = {};
    @Output() back = new EventEmitter<void>();
}
