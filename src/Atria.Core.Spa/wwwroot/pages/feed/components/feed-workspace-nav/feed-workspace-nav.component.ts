import { CommonModule } from '@angular/common';
import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
    ViewEncapsulation,
} from '@angular/core';
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
    imports: [CommonModule, MatIconModule],
})
export class FeedWorkspaceNavComponent implements OnChanges {
    @Input() items: FeedWorkspaceNavItem[] = [];
    @Input() selectedType: TabType | null = null;

    @Output() selectTab = new EventEmitter<TabType>();
    @Output() closeTab = new EventEmitter<TabType>();

    primaryItems: FeedWorkspaceNavItem[] = [];
    componentItems: FeedWorkspaceNavItem[] = [];
    pipelineExpanded = false;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['items']) {
            this.primaryItems = this.items.filter(
                (item) => item.section !== 'components'
            );
            this.componentItems = this.items.filter(
                (item) => item.section === 'components'
            );
        }

        if (changes['selectedType'] && !changes['selectedType'].firstChange) {
            this.pipelineExpanded = false;
        }
    }

    get availableComponentCount(): number {
        return this.componentItems.filter((item) => !item.comingSoon).length;
    }

    get isPipelineSelected(): boolean {
        return this.componentItems.some(
            (item) => item.type === this.selectedType
        );
    }

    mobileLabel(item: FeedWorkspaceNavItem): string {
        return MOBILE_LABELS[item.type] ?? item.label;
    }

    selectMobileItem(item: FeedWorkspaceNavItem): void {
        if (item.comingSoon) {
            return;
        }

        this.pipelineExpanded = false;
        this.selectTab.emit(item.type);
    }

    togglePipeline(): void {
        this.pipelineExpanded = !this.pipelineExpanded;
    }

    closeMobileItem(type: TabType): void {
        this.closeTab.emit(type);
    }

    trackByType(index: number, item: FeedWorkspaceNavItem): TabType {
        return item.type;
    }
}

const MOBILE_LABELS: Partial<Record<TabType, string>> = {
    [TabType.DeployHistory]: 'History',
    [TabType.Result]: 'Preview',
};
