import { MatDialog } from '@angular/material/dialog';
import {
    ConfirmModalComponent,
    ConfirmModalData,
} from '../../../shared/modals/confirm/confirm-modal.component';
import type { FeedWorkspaceNavItem } from '../components/feed-workspace-nav/feed-workspace-nav.component';
import { FEED_TAB_CONFIGS } from '../feed.config';
import { isFeedTabAvailable, TabConfig, TabType } from '../feed.types';

type TabCloseHandler = (tabType: TabType) => void;
type TabSelectionHandler = (tabType: TabType) => void;

export class FeedTabsFacade {
    tabs: TabConfig[] = [];
    selectedTabIndex = 0;

    constructor(
        private readonly _dialog: MatDialog,
        private readonly _isEditMode: () => boolean,
        private readonly _isDraft: () => boolean,
        private readonly _onTabClosed: TabCloseHandler,
        private readonly _functionsEnabled = true,
        private readonly _onTabSelected?: TabSelectionHandler
    ) {}

    get selectedTabType(): TabType | null {
        return this.tabs[this.selectedTabIndex]?.type ?? null;
    }

    get workspaceNavItems(): FeedWorkspaceNavItem[] {
        const navItems = [
            TabType.Settings,
            TabType.Filter,
            TabType.Function,
            TabType.Output,
            TabType.Metrics,
            TabType.Result,
            TabType.DeployHistory,
        ];
        const componentTypes = new Set([
            TabType.Filter,
            TabType.Function,
            TabType.Output,
        ]);

        return navItems
            .filter((type) => isFeedTabAvailable(type, this._isDraft()))
            .filter(
                (type) =>
                    ![TabType.DeployHistory, TabType.Metrics].includes(type) ||
                    this._isEditMode()
            )
            .map((type) => {
                const config = FEED_TAB_CONFIGS[type];
                return {
                    type,
                    label: config.label,
                    icon: config.icon,
                    section: componentTypes.has(type)
                        ? 'components'
                        : 'primary',
                    isAdded: this.hasTab(type),
                    closable: config.closable,
                    comingSoon:
                        type === TabType.Function && !this._functionsEnabled,
                };
            });
    }

    hasTab(tabType: TabType): boolean {
        return this.tabs.some((tab) => tab.type === tabType);
    }

    addTab(tabType: TabType, setActive = true): void {
        if (!isFeedTabAvailable(tabType, this._isDraft())) {
            return;
        }

        if (tabType === TabType.Function && !this._functionsEnabled) {
            return;
        }

        const config = FEED_TAB_CONFIGS[tabType];

        if (!this.hasTab(tabType)) {
            this.tabs.push({ ...config });

            if (setActive) {
                this.selectedTabIndex = this.tabs.findIndex(
                    (tab) => tab.type === tabType
                );
            }
        } else {
            const existingTabIndex = this.tabs.findIndex(
                (tab) => tab.type === tabType
            );

            if (existingTabIndex !== -1) {
                if (setActive) {
                    this.selectedTabIndex = existingTabIndex;
                }
            }
        }

        if (setActive) {
            this._notifySelectionChanged();
        }
    }

    selectTab(index: number): void {
        if (!this.tabs[index]) {
            return;
        }

        this.selectedTabIndex = index;
        this._notifySelectionChanged();
    }

    closeTab(index: number): void {
        const tab = this.tabs[index];

        if (!tab?.closable) {
            return;
        }

        if (tab.requiresConfirmation) {
            const dialogData: ConfirmModalData = {
                title: 'Closing a Tab',
                message: tab.confirmationMessage,
                type: 'warning',
            };

            const dialogRef = this._dialog.open(ConfirmModalComponent, {
                width: '400px',
                data: dialogData,
            });

            dialogRef.afterClosed().subscribe((result) => {
                if (result) {
                    this._performTabClose(index, tab);
                }
            });
        } else {
            this._performTabClose(index, tab);
        }
    }

    closeTabByType(tabType: TabType): void {
        const tabIndex = this.tabs.findIndex((tab) => tab.type === tabType);

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

        this._notifySelectionChanged();
    }

    private _notifySelectionChanged(): void {
        const selectedTabType = this.selectedTabType;

        if (selectedTabType) {
            this._onTabSelected?.(selectedTabType);
        }
    }
}
