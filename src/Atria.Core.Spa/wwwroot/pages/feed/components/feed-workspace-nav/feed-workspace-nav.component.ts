import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TabType } from '../../feed.types';

export interface FeedWorkspaceNavItem {
    type: TabType;
    label: string;
    icon: string;
    section?: 'components' | 'primary';
    isAdded?: boolean;
    closable?: boolean;
    comingSoon?: boolean;
}

@Component({
    selector: 'feed-workspace-nav',
    standalone: true,
    templateUrl: './feed-workspace-nav.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule
    ]
})
export class FeedWorkspaceNavComponent implements OnChanges {
    @Input() items: FeedWorkspaceNavItem[] = [];
    @Input() selectedType: TabType | null = null;

    @Output() selectTab = new EventEmitter<TabType>();
    @Output() closeTab = new EventEmitter<TabType>();

    primaryItems: FeedWorkspaceNavItem[] = [];
    componentItems: FeedWorkspaceNavItem[] = [];

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['items']) {
            this.primaryItems = this.items.filter(item => item.section !== 'components');
            this.componentItems = this.items.filter(item => item.section === 'components');
        }
    }

    trackByType(index: number, item: FeedWorkspaceNavItem): TabType {
        return item.type;
    }
}
