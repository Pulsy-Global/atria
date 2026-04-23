import { Component, EventEmitter, Input, Output, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
    selector: 'feed-header',
    standalone: true,
    templateUrl: './feed-header.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule
    ]
})
export class FeedHeaderComponent {
    @Input() title: string = '';
    @Input() description: string = '';
    @Input() statusLabel: string = '';
    @Input() statusClass: string = '';
    @Output() back = new EventEmitter<void>();
}
