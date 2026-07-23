import { CommonModule, Location } from '@angular/common';
import {
    Component,
    OnDestroy,
    OnInit,
    ViewChild,
    ViewEncapsulation,
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, Subject, takeUntil } from 'rxjs';
import { getFeedErrorDisplayInfo } from '../../shared/config/feed-error-display.config';
import { AppConfigService } from '../../shared/core/config/app.config.service';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { CodeEditorTabComponent } from './components/code-editor-tab/code-editor-tab.component';
import { DeployHistoryTabComponent } from './components/deploy-history-tab/deploy-history-tab.component';
import { FeedConsoleComponent } from './components/feed-console/feed-console.component';
import { FeedHeaderComponent } from './components/feed-header/feed-header.component';
import { FeedSettingsComponent } from './components/feed-settings/feed-settings.component';
import { FeedTabbarComponent } from './components/feed-tabbar/feed-tabbar.component';
import { FeedWorkspaceNavComponent } from './components/feed-workspace-nav/feed-workspace-nav.component';
import { MetricsTabComponent } from './components/metrics-tab/metrics-tab.component';
import { OutputTabComponent } from './components/output-tab/output-tab.component';
import { ResultTabComponent } from './components/result-tab/result-tab.component';
import { FeedExecutionFacade } from './facades/feed-execution.facade';
import { FeedFormFacade } from './facades/feed-form.facade';
import { FeedTabsFacade } from './facades/feed-tabs.facade';
import {
    getFeedTabFragment,
    getFeedTabTypeFromFragment,
} from './feed-tab-fragment';
import { FILTER_TEMPLATES, FUNCTION_TEMPLATES } from './feed.config';
import { FeedService } from './feed.service';
import { TabType } from './feed.types';

@Component({
    selector: 'feeds',
    standalone: true,
    templateUrl: './feed.component.html',
    styleUrls: ['./feed.component.scss'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        MatIconModule,
        FeedHeaderComponent,
        FeedWorkspaceNavComponent,
        FeedTabbarComponent,
        FeedSettingsComponent,
        FeedConsoleComponent,
        CodeEditorTabComponent,
        OutputTabComponent,
        ResultTabComponent,
        DeployHistoryTabComponent,
        MetricsTabComponent,
    ],
})
export class FeedComponent implements OnInit, OnDestroy {
    private readonly _unsubscribeAll: Subject<any> = new Subject<any>();
    private _requestedTabType: TabType | null = null;
    private _canResolveFragment = false;
    private _tabHashReady = false;
    readonly form: FeedFormFacade;
    readonly tabs: FeedTabsFacade;
    readonly execution: FeedExecutionFacade;

    readonly TabType = TabType;
    readonly filterTemplates = FILTER_TEMPLATES;
    readonly functionTemplates = FUNCTION_TEMPLATES;
    readonly getFeedErrorDisplayInfo = getFeedErrorDisplayInfo;
    readonly filterDescription =
        'Process blockchain data and return a custom output (or <code class="bg-hover rounded px-1 py-0.5 text-xs">null</code> to skip), and return a custom result.';
    readonly functionDescription =
        'Process blockchain data or <code class="bg-hover rounded px-1 py-0.5 text-xs">filter</code> output, optionally call APIs or a managed database, and return a custom result.';

    @ViewChild(FeedConsoleComponent, { static: true })
    feedConsole?: FeedConsoleComponent;

    constructor(
        private readonly _activatedRoute: ActivatedRoute,
        private readonly _formBuilder: FormBuilder,
        private readonly _feedService: FeedService,
        private readonly _notificationService: NotificationService,
        private readonly _dialog: MatDialog,
        private readonly _location: Location,
        private readonly _appConfig: AppConfigService,
        private readonly _router: Router
    ) {
        this.form = new FeedFormFacade(
            this._formBuilder,
            this._feedService,
            this._notificationService
        );
        this.tabs = new FeedTabsFacade(
            this._dialog,
            () => this.form.isEditMode(),
            () => this.form.isDraft,
            (tabType) => this.form.handleTabClosed(tabType),
            this._appConfig.functionsEnabled,
            (tabType) => this._syncTabFragment(tabType)
        );
        this.execution = new FeedExecutionFacade(
            this._feedService,
            this._notificationService,
            this._location,
            this.form,
            this._dialog
        );
    }

    ngOnInit(): void {
        const resolverData = this._activatedRoute.snapshot.data['data']
            .feedId as string;
        this._requestedTabType = getFeedTabTypeFromFragment(
            this._activatedRoute.snapshot.fragment
        );

        this.form.bindUnsubscribe(this._unsubscribeAll);
        this.execution.bindUnsubscribe(this._unsubscribeAll);
        this.execution.setConsole(this.feedConsole);
        this.form.setCallbacks(
            () => this.execution.handleFormDirty(),
            () => this.execution.isTestFormDisabled()
        );

        this.form.applyFeedState(resolverData);
        this.form.initializeDeployData();
        this.form.initializeFeedData();
        this.form.initializeForm();
        this.tabs.addTab(TabType.Settings);
        if (
            this._activatedRoute.snapshot.queryParamMap.get('setup') ===
            'output'
        ) {
            this.tabs.addTab(TabType.Output);
        }
        this.form.setupSubscriptions((tabType, setActive) => {
            this.tabs.addTab(tabType, setActive);
        });

        this._activatedRoute.fragment
            .pipe(distinctUntilChanged(), takeUntil(this._unsubscribeAll))
            .subscribe((fragment) => {
                this._requestedTabType = getFeedTabTypeFromFragment(fragment);
                this._restoreRequestedTab();
            });

        this._tabHashReady = true;

        if (this.form.isEditMode()) {
            this._feedService.feed$
                .pipe(takeUntil(this._unsubscribeAll))
                .subscribe((feed) => {
                    if (!feed) {
                        return;
                    }

                    this._canResolveFragment = true;
                    this._restoreRequestedTab();
                });
            this.form.loadFeedData(this.form.feedId!);
        } else {
            this._canResolveFragment = true;
            this._restoreRequestedTab();
        }
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
        this._feedService.clearState();
    }

    onBack(): void {
        this._location.back();
    }

    openTab(tabType: TabType): void {
        this.tabs.addTab(tabType);
    }

    selectTab(index: number): void {
        this.tabs.selectTab(index);
    }

    private _restoreRequestedTab(): void {
        if (!this._tabHashReady || !this._canResolveFragment) {
            return;
        }

        const requestedTabType = this._requestedTabType;

        if (!requestedTabType) {
            const selectedTabType = this.tabs.selectedTabType;

            if (this._activatedRoute.snapshot.fragment && selectedTabType) {
                this._syncTabFragment(selectedTabType);
            }

            return;
        }

        const isAvailable = this.tabs.workspaceNavItems.some(
            (item) => item.type === requestedTabType
        );

        if (!isAvailable) {
            const selectedTabType = this.tabs.selectedTabType;
            this._requestedTabType = null;

            if (selectedTabType) {
                this._syncTabFragment(selectedTabType);
            }

            return;
        }

        if (this.tabs.selectedTabType !== requestedTabType) {
            this.tabs.addTab(requestedTabType);
        }
    }

    private _syncTabFragment(tabType: TabType): void {
        if (!this._tabHashReady) {
            return;
        }

        const fragment = getFeedTabFragment(tabType);

        if (this._activatedRoute.snapshot.fragment === fragment) {
            return;
        }

        void this._router.navigate([], {
            relativeTo: this._activatedRoute,
            fragment,
            queryParamsHandling: 'preserve',
            replaceUrl: true,
        });
    }
}
