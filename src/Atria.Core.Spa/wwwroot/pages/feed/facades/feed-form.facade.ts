import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, takeUntil, timer, forkJoin } from 'rxjs';
import { ERROR_HANDLING_CONFIG } from '../../../shared/config/handling.config';
import { DATA_TYPE_CONFIG } from '../../../shared/config/data-type.config';
import { STRING_EMPTY } from '../../../shared/core/constants/common.constants';
import { FeedValidators } from '../feed.validators';
import { generateFeedName } from '../feed.config';
import { TabType } from '../feed.types';
import { FeedService } from '../feed.service';
import { NotificationService } from '../../../shared/services/notification/notification.service';
import type { EnumOption } from '../../../shared/modals/filter/filter-modal.types';
import {
    AtriaDataType,
    CreateFeed,
    ErrorHandlingStrategy,
    FeedStatus,
    UpdateFeed
} from '../../../api/api.client';
import type {
    Deploy,
    Environment,
    Feed,
    Network,
    Output,
    Tag
} from '../../../api/api.client';

type TabAdder = (tabType: TabType, setActive?: boolean) => void;

export class FeedFormFacade {
    feedForm!: FormGroup;
    testForm!: FormGroup;
    feedId: string | null = null;

    networks: Network[] = [];
    environments: Environment[] = [];
    outputs: Output[] = [];
    feedOutputs: Output[] = [];
    deployHistory: Deploy[] = [];
    tags: Tag[] = [];
    selectedTagIds: string[] = [];
    dataTypeOptions: EnumOption[] = [];
    errorHandlingOptions = ERROR_HANDLING_CONFIG.getStrategies();
    filterCode: string = STRING_EMPTY;
    functionCode: string = STRING_EMPTY;
    currentDeployFeed!: CreateFeed | UpdateFeed;
    isDraft = false;
    feedStatus: FeedStatus | null = null;
    hasFormChanges = false;
    initialFormValue: any = null;
    isLoading = false;

    private _unsubscribeAll?: Subject<any>;
    private _onFormDirty?: () => void;
    private _isTestFormDisabled?: () => boolean;

    constructor(
        private readonly _formBuilder: FormBuilder,
        private readonly _feedService: FeedService,
        private readonly _notificationService: NotificationService
    ) {}

    bindUnsubscribe(unsubscribe$: Subject<any>): void {
        this._unsubscribeAll = unsubscribe$;
    }

    setCallbacks(onFormDirty: () => void, isTestFormDisabled: () => boolean): void {
        this._onFormDirty = onFormDirty;
        this._isTestFormDisabled = isTestFormDisabled;
    }

    applyFeedState(feedId?: string | null): void {
        this.feedId = feedId ?? null;
        this.isLoading = !!this.feedId;
    }

    isEditMode(): boolean {
        return !!this.feedId;
    }

    onNetworkChange(networkTitle: string): void {
        this._updateEnvironments(networkTitle);

        if (this.environments.length > 0) {
            const firstEnvironment = this.environments[0];
            this.feedForm.patchValue({
                environment: firstEnvironment.id,
            });
            this.updateDataTypeOptionsForEnvironment(firstEnvironment.id);
        } else {
            this.feedForm.patchValue({
                environment: STRING_EMPTY,
            });
            this.dataTypeOptions = DATA_TYPE_CONFIG.getDataTypes();
        }
    }

    updateDataTypeOptionsForEnvironment(environmentId: string): void {
        const selectedEnvironment = this.environments.find(e => e.id === environmentId);

        this.dataTypeOptions = DATA_TYPE_CONFIG.getDataTypes(selectedEnvironment?.availableDatasets);

        const currentDataset = this.feedForm.get('dataset')?.value;
        const isCurrentDatasetAvailable = this.dataTypeOptions.some(
            opt => opt.value === currentDataset
        );

        if (!isCurrentDatasetAvailable && this.dataTypeOptions.length > 0) {
            this.feedForm.patchValue({
                dataset: this.dataTypeOptions[0].value,
            });
        }
    }

    onRegenerateName(): void {
        this.feedForm.patchValue({
            name: generateFeedName()
        });
    }

    onFilterCodeChanged(code: string): void {
        this.filterCode = code;
        this._updateDeployFeedModel();
        this._checkFormChanges();
    }

    onFunctionCodeChanged(code: string): void {
        this.functionCode = code;
        this._updateDeployFeedModel();
        this._checkFormChanges();
    }

    onOutputConfigChange(outputs: Output[]): void {
        this.feedOutputs = outputs;
        this._updateDeployFeedModel();
        this._checkFormChanges();
    }

    onTagsChanged(tagIds: string[]): void {
        this.selectedTagIds = tagIds;
        this._updateDeployFeedModel();
        this._checkFormChanges();
    }

    onCreateTag(tagData: { name: string; color: string }): void {
        this._feedService.createTag(tagData.name, tagData.color).subscribe({
            next: (tag: Tag) => {
                const delay$ = timer(300);
                const delayed = this._unsubscribeAll
                    ? delay$.pipe(takeUntil(this._unsubscribeAll))
                    : delay$;

                delayed.subscribe(() => {
                    this.selectedTagIds = [...this.selectedTagIds, tag.id!];
                    this._updateDeployFeedModel();
                });
            },
            error: (error) => {
                this._notificationService.showErrorAlert(
                    'Failed to Create Tag',
                    error.title || 'Failed to create tag'
                );
            }
        });
    }

    getHeaderStatusLabel(): string {
        if (!this.isEditMode()) {
            return 'New';
        }

        if (this.feedStatus) {
            return this.feedStatus;
        }

        return this.isDraft ? 'Draft' : 'Draft';
    }

    getHeaderStatusClass(): string {
        if (!this.isEditMode()) {
            return 'is-warn';
        }

        switch (this.feedStatus) {
            case FeedStatus.Running:
            case FeedStatus.Completed:
                return 'is-good';
            case FeedStatus.Error:
                return 'is-error';
            default:
                return 'is-warn';
        }
    }

    initializeDeployData(deployHistory?: Deploy[] | null): void {
        this.deployHistory = deployHistory || [];
    }

    initializeFeedData(feedData?: Feed | null, addTab?: TabAdder): void {
        const updateData = () => {
            this.isDraft = feedData.status === FeedStatus.Draft;
            this.feedStatus = feedData.status ?? null;

            if (feedData.filterCode) {
                this.filterCode = feedData.filterCode;
                addTab?.(TabType.Filter, false);
            }

            if (feedData.functionCode) {
                this.functionCode = feedData.functionCode;
                addTab?.(TabType.Function, false);
            }

            if (feedData.outputIds && feedData.outputIds.length > 0) {
                addTab?.(TabType.Output, false);
            }
        };

        const resetData = () => {
            this.isDraft = true;
            this.feedStatus = null;
            this.filterCode = STRING_EMPTY;
            this.functionCode = STRING_EMPTY;
            this.deployHistory = [];
            this.feedOutputs = [];
            this.selectedTagIds = [];
        };

        feedData
            ? updateData()
            : resetData();
    }

    initializeForm(feedData?: Feed | null): void {
        this.dataTypeOptions = DATA_TYPE_CONFIG.getDataTypes();
        const initialBlockDelay = feedData?.blockDelay ?? 0;
        const initialBlockDelayMode = initialBlockDelay > 0 ? 'custom' : 'realtime';

        this.feedForm = this._formBuilder.group({
            name: [feedData?.name || generateFeedName(), [
                Validators.required
            ]],
            version: [feedData?.version || '1.0.0', [
                Validators.required,
                FeedValidators.version()
            ]],
            description: [feedData?.description || STRING_EMPTY],
            network: [STRING_EMPTY, [
                Validators.required
            ]],
            environment: [feedData?.networkId || STRING_EMPTY, [
                Validators.required
            ]],
            dataset: [feedData?.dataType || AtriaDataType.BlockWithTransactions, [
                Validators.required
            ]],
            errorHandlingStrategy: [feedData?.errorHandling ||
                                    ErrorHandlingStrategy.ContinueOnError, [
                Validators.required
            ]],
            streamEnd: [feedData?.endBlock
                ? 'endAtSpecificBlock'
                : 'endContinuouslyUntilStopped'],
            streamStart: [feedData?.startBlock
                ? 'startAtSpecificBlock'
                : 'startAtLatestBlock'],
            specificBlockNumber: [feedData?.startBlock || STRING_EMPTY, [
                FeedValidators.blockNumber()
            ]],
            endBlockNumber: [feedData?.endBlock || STRING_EMPTY, [
                FeedValidators.blockNumber()
            ]],
            blockDelayMode: [initialBlockDelayMode],
            blockDelay: [initialBlockDelay, [
                Validators.required,
                Validators.min(0),
                Validators.max(100)
            ]]
        });

        this.testForm = this._formBuilder.group({
            testBlockNumber: [STRING_EMPTY, [
                Validators.required,
                FeedValidators.blockNumber()
            ]],
            executeOutputs: [false]
        });

        if (feedData?.networkId) {
            this._setNetworkAndEnvironmentFromNetworkId(feedData.networkId);
        }

        if (feedData?.tagIds) {
            this.selectedTagIds = [...feedData.tagIds];
        }

        this._updateDeployFeedModel();
        this._captureInitialFormValue();
        this.setupFormSubscriptions();
    }

    setupSubscriptions(addTab: TabAdder): void {
        if (!this._unsubscribeAll) {
            return;
        }

        this._feedService.networks$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((networks: Network[]) => {
                if (networks) {
                    this.networks = networks;

                    if (!this.isEditMode() && networks.length > 0) {
                        const firstNetwork = networks[0];

                        this.feedForm.patchValue({
                            network: firstNetwork.title,
                        }, { emitEvent: false });

                        this._updateEnvironments(firstNetwork.title);

                        if (this.environments.length > 0) {
                            const firstEnvironment = this.environments[0];
                            this.feedForm.patchValue({
                                environment: firstEnvironment.id,
                            }, { emitEvent: false });
                            this.updateDataTypeOptionsForEnvironment(firstEnvironment.id);
                        }

                        this._updateDeployFeedModel();
                        this._captureInitialFormValue();
                    }
                }
            });

        this._feedService.feed$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((feed: Feed) => {
                if (feed) {
                    this.initializeFeedData(feed, addTab);
                    this.initializeForm(feed);
                }
            });

        this._feedService.outputs$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((outputs: Output[]) => {
                if (outputs) {
                    this.outputs = outputs;
                }
            });

        this._feedService.feedOutputs$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((feedOutputs: Output[]) => {
                if (feedOutputs) {
                    this.feedOutputs = feedOutputs;
                    this._updateDeployFeedModel();

                    if (this.isEditMode() && feedOutputs.length > 0) {
                        addTab(TabType.Output, false);
                    }
                }
            });

        this._feedService.deployHistory$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((deployHistory: Deploy[]) => {
                if (deployHistory) {
                    this.initializeDeployData(deployHistory);
                }
            });

        this._feedService.tags$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((tags: Tag[]) => {
                if (tags) {
                    this.tags = tags;
                }
            });
    }

    setupFormSubscriptions(): void {
        if (!this._unsubscribeAll || !this.feedForm) {
            return;
        }

        this.feedForm.valueChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => {
                this._updateDeployFeedModel();
                this._checkFormChanges();
            });

        this.feedForm.get('blockDelayMode')?.valueChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((mode: string) => {
                if (mode === 'realtime') {
                    this.feedForm.get('blockDelay')?.setValue(0);
                }
            });

        this.feedForm.get('blockDelay')?.valueChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((value: number | string | null | undefined) => {
                if (value === null || value === undefined || value === '') {
                    return;
                }

                const numericValue = Number(value);
                if (!Number.isFinite(numericValue)) {
                    return;
                }

                const expectedMode = numericValue > 0 ? 'custom' : 'realtime';
                const modeControl = this.feedForm.get('blockDelayMode');

                if (modeControl?.value !== expectedMode) {
                    modeControl?.setValue(expectedMode, { emitEvent: false });
                }
            });

        this.feedForm.statusChanges
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => {
                this._updateTestFormState();
            });
    }

    handleTabClosed(tabType: TabType): void {
        const tabCloseActions = {
            [TabType.Settings]: () => {
            },
            [TabType.Filter]: () => {
                this.filterCode = STRING_EMPTY;
                this.onFilterCodeChanged(STRING_EMPTY);
            },
            [TabType.Function]: () => {
                this.functionCode = STRING_EMPTY;
                this.onFunctionCodeChanged(STRING_EMPTY);
            },
            [TabType.Output]: () => {
                this.feedOutputs = [];
                this.onOutputConfigChange([]);
            },
            [TabType.DeployHistory]: () => {
            }
        };

        tabCloseActions[tabType]();
    }

    loadFeedData(feedId: string): void {
        if (!this._unsubscribeAll) {
            return;
        }

        this.isLoading = true;

        forkJoin([
            this._feedService.getFeed(feedId),
            this._feedService.getFeedOutputs(feedId),
            this._feedService.getDeployHistory(feedId)
        ]).subscribe({
            next: () => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;
                    });
            },
            error: () => {
                this._notificationService.showErrorAlert(
                    'Error Loading Feed',
                    'Failed to load feed data'
                );

                this.isLoading = false;
            }
        });
    }

    markSaved(): void {
        this.hasFormChanges = false;
        this._captureInitialFormValue();
    }

    private _updateEnvironments(networkTitle: string): void {
        const selectedNetwork = this.networks
            .find(network => network.title === networkTitle);

        this.environments = selectedNetwork?.environments || [];
    }

    private _setNetworkAndEnvironmentFromNetworkId(networkId: string): void {
        const network = this.networks.find(net =>
            net.environments?.some(env => env.id === networkId)
        );

        if (!network) {
            return;
        }

        const environment = network.environments
            ?.find(env => env.id === networkId);

        this.feedForm.patchValue({
            network: network.title,
            environment: environment?.id,
        });

        this._updateEnvironments(network.title);

        if (environment) {
            this.updateDataTypeOptionsForEnvironment(environment.id!);
        }
    }

    private _captureInitialFormValue(): void {
        this.initialFormValue = {
            form: this.feedForm.getRawValue(),
            filterCode: this.filterCode,
            functionCode: this.functionCode,
            outputs: [...this.feedOutputs.map(o => o.id)],
            tags: [...this.selectedTagIds]
        };
    }

    private _checkFormChanges(): void {
        if (!this.initialFormValue) {
            this.hasFormChanges = true;
            this._onFormDirty?.();
            return;
        }

        const currentValue = {
            form: this.feedForm.getRawValue(),
            filterCode: this.filterCode,
            functionCode: this.functionCode,
            outputs: [...this.feedOutputs.map(o => o.id)],
            tags: [...this.selectedTagIds]
        };

        const filterChanged = this.initialFormValue.filterCode !== currentValue.filterCode;
        const functionChanged = this.initialFormValue.functionCode !== currentValue.functionCode;

        const formChanged = JSON.stringify(this.initialFormValue.form) !==
                            JSON.stringify(currentValue.form);

        const outputsChanged = JSON.stringify(this.initialFormValue.outputs.sort()) !==
                               JSON.stringify(currentValue.outputs.sort());

        const tagsChanged = JSON.stringify(this.initialFormValue.tags.sort()) !==
                            JSON.stringify(currentValue.tags.sort());

        this.hasFormChanges = formChanged || filterChanged || functionChanged || outputsChanged || tagsChanged;

        if (this.hasFormChanges) {
            this._onFormDirty?.();
        }
    }

    private _updateDeployFeedModel(): void {
        const formValue = this.feedForm.value;

        this.currentDeployFeed = this.isEditMode()
            ? new UpdateFeed()
            : new CreateFeed();

        const blockNumbers = {
            startBlock: formValue.streamStart === 'startAtSpecificBlock' &&
                        formValue.specificBlockNumber
                ? parseInt(formValue.specificBlockNumber)
                : undefined,
            endBlock: formValue.streamEnd === 'endAtSpecificBlock' &&
                      formValue.endBlockNumber
                ? parseInt(formValue.endBlockNumber)
                : undefined
        };

        Object.assign(this.currentDeployFeed, {
            name: formValue.name || STRING_EMPTY,
            version: formValue.version || STRING_EMPTY,
            description: formValue.description || STRING_EMPTY,
            networkId: formValue.environment || STRING_EMPTY,
            dataType: formValue.dataset,
            errorHandling: formValue.errorHandlingStrategy,
            filterCode: this.filterCode || undefined,
            functionCode: this.functionCode || undefined,
            outputIds: this.feedOutputs.map(output => output.id!),
            tagIds: this.selectedTagIds,
            blockDelay: formValue.blockDelayMode === 'realtime' ? 0 : (formValue.blockDelay || 0),
            ...blockNumbers
        });
    }

    private _updateTestFormState(): void {
        if (!this.testForm || !this._isTestFormDisabled) {
            return;
        }

        const shouldDisable = this._isTestFormDisabled();

        const testBlockNumberControl = this.testForm.get('testBlockNumber');
        const executeOutputsControl = this.testForm.get('executeOutputs');

        if (!testBlockNumberControl || !executeOutputsControl) {
            return;
        }

        if (shouldDisable) {
            if (testBlockNumberControl.enabled) {
                testBlockNumberControl.disable({ emitEvent: false });
            }
            if (executeOutputsControl.enabled) {
                executeOutputsControl.disable({ emitEvent: false });
            }
        } else {
            if (testBlockNumberControl.disabled) {
                testBlockNumberControl.enable({ emitEvent: false });
            }
            if (executeOutputsControl.disabled) {
                executeOutputsControl.enable({ emitEvent: false });
            }
        }
    }
}
