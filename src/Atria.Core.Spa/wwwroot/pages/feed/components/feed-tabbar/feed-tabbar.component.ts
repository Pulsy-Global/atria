import { Component, EventEmitter, Input, Output, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TabConfig } from '../../feed.types';

@Component({
    selector: 'feed-tabbar',
    standalone: true,
    templateUrl: './feed-tabbar.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule
    ]
})
export class FeedTabbarComponent {
    @Input() tabs: TabConfig[] = [];
    @Input() selectedIndex = 0;

    @Output() selectTab = new EventEmitter<number>();
    @Output() closeTab = new EventEmitter<number>();
}
