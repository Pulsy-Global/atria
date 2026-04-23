import { Component, OnDestroy, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { FeedService } from './feed.service';
import { TabType } from './feed.types';
import { FILTER_TEMPLATES, FUNCTION_TEMPLATES } from './feed.config';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { CodeEditorTabComponent } from './components/code-editor-tab/code-editor-tab.component';
import { OutputTabComponent } from './components/output-tab/output-tab.component';
import { ResultTabComponent } from './components/result-tab/result-tab.component';
import { DeployHistoryTabComponent } from './components/deploy-history-tab/deploy-history-tab.component';
import { FeedHeaderComponent } from './components/feed-header/feed-header.component';
import { FeedWorkspaceNavComponent } from './components/feed-workspace-nav/feed-workspace-nav.component';
import { FeedTabbarComponent } from './components/feed-tabbar/feed-tabbar.component';
import { FeedSettingsComponent } from './components/feed-settings/feed-settings.component';
import { FeedConsoleComponent } from './components/feed-console/feed-console.component';
import { Subject } from 'rxjs';
import { FeedExecutionFacade } from './facades/feed-execution.facade';
import { FeedFormFacade } from './facades/feed-form.facade';
import { FeedTabsFacade } from './facades/feed-tabs.facade';
import { AppConfigService } from '../../shared/core/config/app.config.service';

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
        DeployHistoryTabComponent
    ]
})
export class FeedComponent implements OnInit, OnDestroy {

    private readonly _unsubscribeAll: Subject<any> = new Subject<any>();
    readonly form: FeedFormFacade;
    readonly tabs: FeedTabsFacade;
    readonly execution: FeedExecutionFacade;

    readonly TabType = TabType;
    readonly filterTemplates = FILTER_TEMPLATES;
    readonly functionTemplates = FUNCTION_TEMPLATES;
    readonly filterDescription = 'Process blockchain data and return a custom output (or <code class="bg-hover rounded px-1 py-0.5 text-xs">null</code> to skip), and return a custom result.';
    readonly functionDescription = 'Process blockchain data or <code class="bg-hover rounded px-1 py-0.5 text-xs">filter</code> output, optionally call APIs or a managed database, and return a custom result.';

    @ViewChild(FeedConsoleComponent, { static: true }) feedConsole?: FeedConsoleComponent;

    constructor(
        private readonly _activatedRoute: ActivatedRoute,
        private readonly _formBuilder: FormBuilder,
        private readonly _feedService: FeedService,
        private readonly _notificationService: NotificationService,
        private readonly _dialog: MatDialog,
        private readonly _location: Location,
        private readonly _appConfig: AppConfigService,
    ) {
        this.form = new FeedFormFacade(
            this._formBuilder,
            this._feedService,
            this._notificationService
        );
        this.tabs = new FeedTabsFacade(
            this._dialog,
            () => this.form.isEditMode(),
            (tabType) => this.form.handleTabClosed(tabType),
            this._appConfig.functionsEnabled
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
        const resolverData = this._activatedRoute.snapshot
            .data['data'].feedId as string;

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
        this.form.setupSubscriptions((tabType, setActive) => {
            this.tabs.addTab(tabType, setActive);
        });

        if (this.form.isEditMode()) {
            this.form.loadFeedData(this.form.feedId!);
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
}
