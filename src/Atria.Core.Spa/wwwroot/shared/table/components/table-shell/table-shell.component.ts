import { CommonModule } from '@angular/common';
import {
    Component,
    EventEmitter,
    Input,
    Output,
    ViewEncapsulation,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'atria-table-shell',
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatIconModule],
    templateUrl: './table-shell.component.html',
    styleUrls: ['./table-shell.component.scss'],
    encapsulation: ViewEncapsulation.None,
    host: {
        class: 'flex min-h-0 flex-1 flex-col',
        '[class.is-loading]': 'loading',
    },
})
export class TableShellComponent {
    @Input() loading = false;
    @Input() error = false;
    @Input() empty = false;
    @Input() emptyTitle = 'No results found';
    @Input() emptyDescription = '';
    @Input() errorTitle = 'Unable to load data';
    @Input() skeletonRows = 10;

    @Output() readonly retry = new EventEmitter<void>();

    get skeleton(): number[] {
        return Array.from({ length: this.skeletonRows }, (_, index) => index);
    }
}
