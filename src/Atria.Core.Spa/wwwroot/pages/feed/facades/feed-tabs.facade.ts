import { MatDialog } from '@angular/material/dialog';
import type { FeedWorkspaceNavItem } from '../components/feed-workspace-nav/feed-workspace-nav.component';
import { FEED_TAB_CONFIGS } from '../feed.config';
import { TabConfig, TabType } from '../feed.types';
import { ConfirmModalComponent, ConfirmModalData } from '../../../shared/modals/confirm/confirm-modal.component';

type TabCloseHandler = (tabType: TabType) => void;

export class FeedTabsFacade {
    tabs: TabConfig[] = [];
    selectedTabIndex = 0;

    constructor(
        private readonly _dialog: MatDialog,
        private readonly _isEditMode: () => boolean,
        private readonly _onTabClosed: TabCloseHandler,
        private readonly _functionsEnabled = true
    ) {}

    get workspaceNavItems(): FeedWorkspaceNavItem[] {
        const navItems = [
            TabType.Settings,
            TabType.Filter,
            TabType.Function,
            TabType.Output,
            TabType.DeployHistory,
            TabType.Result
        ];
        const componentTypes = new Set([
            TabType.Filter,
            TabType.Function,
            TabType.Output
        ]);

        return navItems
            .filter(type => type !== TabType.DeployHistory || this._isEditMode())
            .map(type => {
                const config = FEED_TAB_CONFIGS[type];
                return {
                    type,
                    label: config.label,
                    icon: config.icon,
                    section: componentTypes.has(type) ? 'components' : 'primary',
                    isAdded: this.hasTab(type),
                    closable: config.closable,
                    comingSoon: type === TabType.Function && !this._functionsEnabled,
                };
            });
    }

    hasTab(tabType: TabType): boolean {
        return this.tabs.some(tab => tab.type === tabType);
    }

    addTab(tabType: TabType, setActive = true): void {
        if (tabType === TabType.Function && !this._functionsEnabled) return;

        const config = FEED_TAB_CONFIGS[tabType];

        if (!this.hasTab(tabType)) {
            this.tabs.push({ ...config });

            if (setActive) {
                this.selectedTabIndex = this.tabs
                    .findIndex(tab => tab.type === tabType);
            }
        } else {
            const existingTabIndex = this.tabs
                .findIndex(tab => tab.type === tabType);

            if (existingTabIndex !== -1) {
                if (setActive) {
                    this.selectedTabIndex = existingTabIndex;
                }
            }
        }
    }

    closeTab(index: number): void {
        const tab = this.tabs[index];

        if (!tab?.closable) return;

        if (tab.requiresConfirmation) {
            const dialogData: ConfirmModalData = {
                title: 'Closing a Tab',
                message: tab.confirmationMessage,
                type: 'warning'
            };

            const dialogRef = this._dialog.open(ConfirmModalComponent, {
                width: '400px',
                data: dialogData
            });

            dialogRef.afterClosed().subscribe(result => {
                if (result) {
                    this._performTabClose(index, tab);
                }
            });
        } else {
            this._performTabClose(index, tab);
        }
    }

    closeTabByType(tabType: TabType): void {
        const tabIndex = this.tabs.findIndex(tab => tab.type === tabType);

        if (tabIndex !== -1) {
            this.closeTab(tabIndex);
        }
    }

    private _performTabClose(index: number, tab: TabConfig): void {
        this.tabs.splice(index, 1);
        this._onTabClosed(tab.type);

        if (this.selectedTabIndex >= this.tabs.length) {
            this.selectedTabIndex = Math.max(0, this.tabs.length - 1);
        }
    }
}
