import { Location } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { Observable, Subject, map, switchMap, takeUntil, tap, timer } from 'rxjs';
import { FeedService } from '../feed.service';
import { NotificationService } from '../../../shared/services/notification/notification.service';
import { FeedConsoleComponent } from '../components/feed-console/feed-console.component';
import type { CreateFeed, Feed, TestResult, UpdateFeed } from '../../../api/api.client';
import { TestRequest } from '../../../api/api.client';
import { FeedFormFacade } from './feed-form.facade';
import {
    isCursorBehindTailError,
    openCursorResetConfirm
} from '../../../shared/modals/cursor-reset/cursor-reset.helper';

export class FeedExecutionFacade {
    isTestRunning = false;
    isLoadingLatestBlock = false;
    isDeploying = false;
    deploySucceeded = false;
    deployFailed = false;
    testPassed = false;
    testFailed = false;
    shouldAutoRunTestAfterBlockLoad = false;

    private _console?: FeedConsoleComponent;
    private _unsubscribeAll?: Subject<any>;

    constructor(
        private readonly _feedService: FeedService,
        private readonly _notificationService: NotificationService,
        private readonly _location: Location,
        private readonly _form: FeedFormFacade,
        private readonly _dialog: MatDialog
    ) {}

    bindUnsubscribe(unsubscribe$: Subject<any>): void {
        this._unsubscribeAll = unsubscribe$;
    }

    setConsole(console?: FeedConsoleComponent): void {
        this._console = console;
    }

    handleFormDirty(): void {
        this.testPassed = false;
        this.testFailed = false;
        this._console?.clearOutput();
    }

    isDeployDisabled(): boolean {
        const isFormInvalid = !this._form.feedForm.valid;

        return isFormInvalid ||
               !this.testPassed ||
               this.isTestRunning ||
               this._form.isLoading ||
               this.isDeploying;
    }

    isTestFormDisabled(): boolean {
        const isFormInvalid = !this._form.feedForm.valid;

        return isFormInvalid ||
               this.isTestRunning ||
               this._form.isLoading;
    }

    shouldShowTestButton(): boolean {
        return !this.testPassed;
    }

    shouldShowDeployButton(): boolean {
        return this.testPassed;
    }

    canRunTest(): boolean {
        const isFormValid = this._form.feedForm.valid;
        const isTestValid = this._form.testForm.valid;

        return isFormValid &&
               isTestValid &&
               !this.isTestRunning &&
               !this._form.isLoading;
    }

    canAttemptTest(): boolean {
        const isFormValid = this._form.feedForm.valid;

        return isFormValid &&
               !this.isTestRunning &&
               !this._form.isLoading &&
               !this.isLoadingLatestBlock;
    }

    canLoadLatestBlock(): boolean {
        const hasEnvironment = !!this._form.feedForm.get('environment')?.value;
        const formDisabled = this.isTestFormDisabled();

        return hasEnvironment &&
               !formDisabled &&
               !this.isLoadingLatestBlock;
    }

    hasOutputs(): boolean {
        return this._form.feedOutputs.length > 0;
    }

    onRunTestClick(): void {
        this._console?.open();

        if (!this.canAttemptTest()) {
            return;
        }

        const blockValue = this._form.testForm.get('testBlockNumber')?.value;
        const hasBlockNumber = !!blockValue && blockValue.toString().trim().length > 0;

        if (!hasBlockNumber) {
            this.shouldAutoRunTestAfterBlockLoad = true;
            this.loadLatestBlock();
            return;
        }

        if (!this.canRunTest()) {
            return;
        }

        this.runTest();
    }

    onTestFeedClick(): void {
        this.onRunTestClick();
    }

    runTest(): void {
        const testBlockNumber = this._form.testForm.get('testBlockNumber')?.value;
        const shouldExecuteOutputs = this.hasOutputs() && this._form.testForm.get('executeOutputs')?.value;

        this.isTestRunning = true;
        this._console?.open();
        this._console?.logTestStart();

        const testArgs = new TestRequest({
            blockchainId: this._form.feedForm.get('environment')?.value,
            dataType: this._form.feedForm.get('dataset')?.value,
            blockNumber: testBlockNumber,
            filterCode: this._form.filterCode || undefined,
            functionCode: this._form.functionCode || undefined,
            executeOutputs: shouldExecuteOutputs,
            outputsIds: this._form.feedOutputs.map(o => o.id!)
        });

        this._feedService.testFeed(testArgs).subscribe({
            next: (result: TestResult) => {
                const delay$ = timer(300);
                const delayed = this._unsubscribeAll
                    ? delay$.pipe(takeUntil(this._unsubscribeAll))
                    : delay$;

                delayed.subscribe(() => {
                    this._handleTestSuccess(result);
                });
            },
            error: (error) => {
                this._handleTestError(error);
            }
        });
    }

    loadLatestBlock(): void {
        const environmentId = this._form.feedForm.get('environment')?.value;

        if (!environmentId) {
            this._notificationService.showWarningAlert(
                'Select Network',
                'Please select a network and environment first'
            );
            return;
        }

        this.isLoadingLatestBlock = true;

        this._feedService.getLatestBlock(environmentId).subscribe({
            next: (latestBlock) => {
                this._form.testForm.patchValue({
                    testBlockNumber: latestBlock.blockNumber.toString()
                });

                this.isLoadingLatestBlock = false;

                if (this.shouldAutoRunTestAfterBlockLoad && this.canRunTest()) {
                    this.runTest();
                }

                this.shouldAutoRunTestAfterBlockLoad = false;
            },
            error: (error) => {
                this._notificationService.showErrorAlert(
                    'Failed to Load Block',
                    error.title || 'Could not fetch latest block'
                );

                this.isLoadingLatestBlock = false;
                this.shouldAutoRunTestAfterBlockLoad = false;
            }
        });
    }

    deployFeed(): void {
        this.isDeploying = true;
        this.deploySucceeded = false;
        this.deployFailed = false;

        const deployObservable = this._form.isEditMode()
            ? this._feedService.updateFeed(this._form.feedId!, this._form.currentDeployFeed as UpdateFeed)
            : this._feedService.createFeed(this._form.currentDeployFeed as CreateFeed);

        let deployedFeed: Feed | null = null;
        let emittedNext = false;

        deployObservable.pipe(
            tap((feed) => { deployedFeed = feed; }),
            switchMap((feed) => {
                return this._feedService.startFeed(feed.id!).pipe(
                    map(() => feed)
                );
            })
        ).subscribe({
            next: (feed) => {
                emittedNext = true;
                this._finalizeDeploySuccess(feed);
            },
            error: (error) => {
                if (deployedFeed && isCursorBehindTailError(error)) {
                    this._showCursorResetModal(deployedFeed);
                } else {
                    this._handleDeployError(error);
                }
            },
            complete: () => {
                if (!emittedNext && this.isDeploying) {
                    this.isDeploying = false;
                    this.deployFailed = true;
                }
            }
        });
    }

    private _showCursorResetModal(feed: Feed): void {
        this._boundedSubscribe(
            openCursorResetConfirm(this._dialog, feed.name || 'Unknown'),
            confirmed => {
                if (confirmed) {
                    this._performCursorReset(feed);
                } else {
                    this.isDeploying = false;
                    this.deployFailed = true;
                }
            }
        );
    }

    private _performCursorReset(feed: Feed): void {
        this._feedService.resetCursorAndStart(feed.id!).subscribe({
            next: () => this._finalizeDeploySuccess(feed),
            error: (error) => this._handleDeployError(error)
        });
    }

    private _finalizeDeploySuccess(feed: Feed): void {
        this._boundedSubscribe(timer(300), () => {
            this._handleDeploySuccess(feed);
        });
    }

    private _boundedSubscribe<T>(source$: Observable<T>, next: (value: T) => void): void {
        const bounded$ = this._unsubscribeAll
            ? source$.pipe(takeUntil(this._unsubscribeAll))
            : source$;

        bounded$.subscribe(next);
    }

    private _handleTestSuccess(result: TestResult): void {
        this._console?.logTestSuccess(result);

        if (!result.filterError && !result.functionError) {
            this.testPassed = true;
            this.testFailed = false;
        } else {
            this.testPassed = false;
            this.testFailed = true;
        }

        this.isTestRunning = false;
    }

    private _handleTestError(error: any): void {
        this._console?.logTestError(error);

        this.testPassed = false;
        this.testFailed = true;
        this.isTestRunning = false;

        this._notificationService.showErrorAlert(
            'Test Failed',
            error.title || 'Failed to execute test'
        );
    }

    private _handleDeploySuccess(feed: Feed): void {
        if (!this._form.isEditMode()) {
            this._location.replaceState(`/feed/${feed.id}`);
        }

        this._notificationService.showSuccessAlert(
            'Deploy Complete',
            'Feed deployed and started successfully!'
        );

        this._form.applyFeedState(feed.id);
        this._form.loadFeedData(feed.id);

        this._console?.clearOutput();
        this._console?.collapse();
        this.isDeploying = false;
        this.deploySucceeded = true;
        this.deployFailed = false;
        this.testPassed = false;
        this.testFailed = false;

        this._form.markSaved();
    }

    private _handleDeployError(error: any): void {
        this._notificationService.showErrorAlert(
            `Deploy Failed`,
            error.title
        );

        this.isDeploying = false;
        this.deploySucceeded = false;
        this.deployFailed = true;
    }
}
