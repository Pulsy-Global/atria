import { Component, OnInit, OnDestroy, ViewEncapsulation, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { FuseCardComponent } from 'fuse/components/card';
import { FeedTableService } from './feed-table.service';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { ConfirmModalComponent, ConfirmModalData } from '../../shared/modals/confirm/confirm-modal.component';
import { openCursorResetConfirm, isCursorBehindTailError } from '../../shared/modals/cursor-reset/cursor-reset.helper';
import { FilterModalComponent } from '../../shared/modals/filter/filter-modal.component';
import { Subject, takeUntil, debounceTime, distinctUntilChanged, combineLatest, timer, interval, filter, finalize } from 'rxjs';
import { FuseLoadingService } from 'fuse/services/loading';
import { Feed, FeedStatus, AtriaDataType, Network, Environment, Tag } from '../../api/api.client';
import { ColumnConfig, TableState, PaginationState } from '../../shared/table/table.types';
import { SearchBarComponent } from '../../shared/core/search/search.component'
import { AtriaPaginationDirective } from '../../shared/core/paginator/paginator.directive'
import { FilterModalData, FilterModalResult, FilterElement } from '../../shared/modals/filter/filter-modal.types';
import { FilterType } from '../../shared/table/odata.types';
import { FEED_TABLE_PAGE_KEY, STRING_EMPTY } from '../../shared/core/constants/common.constants';
import { TableStateService } from '../../shared/table/services/table.state.service';
import { TableFilterService } from '../../shared/table/services/table.filter.service';
import { TableSearchService } from '../../shared/table/services/table.search.service';
import { TableOrderService } from '../../shared/table/services/table.order.service';
import { TablePaginationService } from '../../shared/table/services/table.pagination.service';
import { TableODataService } from '../../shared/table/services/table.odata.service';
import { FEED_TABLE_CONFIG, STATUS_CONFIG, BLOCK_CONFIG } from './feed-table.config';
import { DATA_TYPE_CONFIG } from '../../shared/config/data-type.config';

@Component({
    selector: 'feed-table',
    standalone: true,
    templateUrl: './feed-table.component.html',
    styleUrls: ['./feed-table.component.scss'],
    encapsulation: ViewEncapsulation.None,
    providers: [
        TableFilterService,
        TableSearchService,
        TableOrderService,
        TablePaginationService,
        TableODataService
    ],
    imports: [
        CommonModule,
        RouterModule,
        MatTableModule,
        MatSortModule,
        MatButtonModule,
        MatIconModule,
        MatInputModule,
        MatFormFieldModule,
        MatSlideToggleModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
        MatPaginatorModule,
        MatChipsModule,
        FuseCardComponent,
        SearchBarComponent,
        AtriaPaginationDirective
    ]
})
export class FeedTableComponent implements OnInit, OnDestroy {

    private _unsubscribeAll: Subject<any> = new Subject<any>();

    isLoading = true;
    isLargeScreen: boolean = true;

    feeds: Feed[] = [];
    networks: Network[] = [];
    tags: Tag[] = [];
    total: number = 0;

    displayedColumns: string[] = [];
    tableState: TableState;
    streamData: { [key: string]: { feedCursor: number, chainHead: number } } = {};

    readonly FeedStatus = FeedStatus;
    readonly FilterType = FilterType;

    scrollbarTableOptions = {
        suppressScrollX: true,
        suppressScrollY: false,
        scrollYMarginOffset: 80,
        wheelPropagation: false,
        swipeEasing: true
    };

    constructor(
        private readonly _activatedRoute: ActivatedRoute,
        private readonly _feedTableService: FeedTableService,
        private readonly _notificationService: NotificationService,
        private readonly _tableStateService: TableStateService,
        private readonly _filterService: TableFilterService,
        private readonly _searchService: TableSearchService,
        private readonly _odataService: TableODataService,
        private readonly _orderService: TableOrderService,
        private readonly _paginationService: TablePaginationService,
        private readonly _dialog: MatDialog,
        private readonly _router: Router,
        private readonly _fuseLoadingService: FuseLoadingService,
    ) {
        this.displayedColumns = FEED_TABLE_CONFIG
            .getFields()
            .map(col => col.key);

        this._checkScreenSize();
    }

    @HostListener('window:resize', ['$event'])
    onResize(event: any): void {
        this._checkScreenSize();
    }

    ngOnInit(): void {
        const resolverData = this._activatedRoute.snapshot
            .data['data'].table as TableState;

        this._applyTableState(resolverData);
        this._setupSubscriptions();
        this._setupReactiveUpdates();
        this._setupPolling();
        this._loadFeeds();
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
        this._feedTableService.clearState();
    }

    hasFilters(): boolean {
        return this.tableState?.filters?.length > 0;
    }

    onSearchChange(searchTerm: string): void {
        this._searchService.setSearchTerm(searchTerm);
    }

    onMatSortChange(sort: Sort): void {
        this._orderService.setSort(sort);
    }

    onFilter(column: string, event: MouseEvent): void {
        event.stopPropagation();

        const config = FEED_TABLE_CONFIG.getField(column);

        if (!config || config.filterType === FilterType.None)
            return;

        const currentFilter = this._filterService.getCurrentFilter(column);
        const enumOptions = this._getEnumOptions(column);

        const modalData: FilterModalData = {
            columnConfig: config,
            currentFilter: currentFilter,
            enumOptions: enumOptions,
            tagOptions: this.tags
        };

        const dialogRef = this._dialog.open(FilterModalComponent, {
            width: '400px',
            data: modalData,
            autoFocus: false,
            restoreFocus: false
        });

        dialogRef.afterClosed().subscribe(
            (result: FilterModalResult | null) => {
                if (result) {
                    this._filterService.addFilterFromModal(result);
                }
            });
    }

    onRemoveFilter(field: string): void {
        this._filterService.removeFilter(field);
    }

    onClearAllFilters(): void {
        this._filterService.clearAllFilters();
    }

    onPageChange(event: PageEvent): void {
        this._paginationService.setPagination(
            event.pageIndex, event.pageSize);
    }

    onToggleStatus(feed: Feed, event?: MatSlideToggleChange): void {
        const currentState = this.isFeedRunning(feed);

        if (event?.source) {
            event.source.checked = currentState;
        }

        let action: string;
        let serviceCall: any;

        switch (feed.status) {
            case FeedStatus.Running:
                action = 'pause';
                serviceCall = this._feedTableService.pauseFeed(feed);
                break;
            case FeedStatus.Paused:
                action = 'resume';
                serviceCall = this._feedTableService.startFeed(feed);
                break;
            default:
                return;
        }

        serviceCall
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe({
                next: () => {
                    if (event?.source) {
                        event.source.checked = !currentState;
                    }

                    timer(500)
                        .pipe(takeUntil(this._unsubscribeAll))
                        .subscribe(() => {
                            this._notificationService.showSuccessAlert(
                                `Feed ${action}d`,
                                `"${feed.name}" has been successfully ${action}d.`
                            );
                        });
                },
                error: (error) => {
                    if (event?.source) {
                        event.source.checked = currentState;
                    }

                    if (isCursorBehindTailError(error)) {
                        this._showCursorResetModal(feed);
                    } else {
                        this._notificationService.showErrorAlert(
                            `Failed to ${action} feed`,
                            error.title || `Failed to ${action} feed "${feed.name}"`
                        );
                    }
                }
            });
    }

    onCreateFeed(): void {
        this._router.navigate(['/feed']);
    }

    onEditFeed(feed: Feed): void {
        this._router.navigate(['/feed', feed.id]);
    }

    onDeleteFeed(feed: Feed): void {
        const dialogData: ConfirmModalData = {
            title: 'Delete Feed',
            message: `Are you sure you want to delete "${feed.name}"? This action cannot be undone.`,
            type: 'danger'
        };

        const dialogRef = this._dialog.open(ConfirmModalComponent, {
            width: '400px',
            data: dialogData,
            autoFocus: false,
            restoreFocus: false
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this._performDelete(feed);
            }
        });
    }

    isFeedRunning(feed: Feed): boolean {
        return feed.status === FeedStatus.Running;
    }

    isFeedActive(feed: Feed): boolean {
        return feed.status === FeedStatus.Running ||
               feed.status === FeedStatus.Paused;
    }

    isSortable(field: string): boolean {
        return FEED_TABLE_CONFIG.getField(field)?.sortable || false;
    }

    isFiltrable(field: string): boolean {
        return FEED_TABLE_CONFIG.getField(field)?.filterType !== FilterType.None;
    }

    isActiveFilter(field: string): boolean {
        return this.tableState?.filters?.some(f => f.field === field) || false;
    }

    getStatusColor(status: FeedStatus): string {
        return STATUS_CONFIG.getStatusColor(status);
    }

    getStatusText(status: FeedStatus): string {
        return STATUS_CONFIG.getStatusLabel(status);
    }

    getFeedLag(feed: any): number {
        const data = this.streamData[feed.id];
        if (!data) return 0;
        return Math.max(0, data.chainHead - data.feedCursor - (feed.blockDelay || 0));
    }

    getDataTypeColor(dataType: AtriaDataType): string {
        return DATA_TYPE_CONFIG.getDataTypeColor(dataType);
    }

    getNetworkTitle(networkId: string): string {
        if (!this.networks || !networkId) return 'Unknown';

        for (const network of this.networks) {
            const environment = network.environments?.find(env => env.id === networkId);
            if (environment) {
                return network.title || 'Unknown';
            }
        }
        return 'Unknown';
    }

    getEnvironmentTitle(networkId: string): string {
        if (!this.networks || !networkId) return 'Unknown';

        for (const network of this.networks) {
            const environment = network.environments?.find(env => env.id === networkId);
            if (environment) {
                return environment.title || 'Unknown';
            }
        }
        return 'Unknown';
    }

    getTagName(tagId: string): string {
        const tag = this.tags.find(t => t.id === tagId);
        return tag?.name || 'Unknown';
    }

    getTagColor(tagId: string): string {
        const tag = this.tags.find(t => t.id === tagId);
        return tag?.color || '#3B82F6';
    }

    getValidTagIds(tagIds: string[] | undefined): string[] {
        if (!tagIds || tagIds.length === 0) return [];
        return tagIds.filter(tagId => this.tags.some(t => t.id === tagId));
    }

    formatFieldValue(field: string, value: any): string {
        switch (field) {
            case 'status':
                return STATUS_CONFIG.getStatusLabel(value);
            case 'dataType':
                return DATA_TYPE_CONFIG.getDataTypeLabel(value);
            case 'startBlock':
                return BLOCK_CONFIG.getStartBlockLabel(value);
            case 'endBlock':
                return BLOCK_CONFIG.getEndBlockLabel(value);
            default:
                return String(value || STRING_EMPTY);
        }
    }

    trackByFeedId(index: number, item: Feed): any {
        return item.id;
    }

    private _showCursorResetModal(feed: Feed): void {
        openCursorResetConfirm(this._dialog, feed.name || 'Unknown')
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(confirmed => {
                if (confirmed) {
                    this._performCursorReset(feed);
                }
            });
    }

    private _performCursorReset(feed: Feed): void {
        this._feedTableService.resetCursorAndStart(feed)
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe({
                next: () => {
                    this._notificationService.showSuccessAlert(
                        'Cursor Reset',
                        `"${feed.name}" cursor has been reset and feed restarted.`
                    );
                },
                error: (error) => {
                    this._notificationService.showErrorAlert(
                        'Reset Failed',
                        error.title || `Failed to reset cursor for "${feed.name}"`
                    );
                }
            });
    }

    private _checkScreenSize(): void {
        this.isLargeScreen = window.innerWidth >= 1280;
    }

    private _getEnumOptions(field: string) {
        switch (field) {
            case 'status':
                return STATUS_CONFIG.getStatuses();
            case 'dataType':
                return DATA_TYPE_CONFIG.getDataTypes();
            case 'networkId':
                return this._getNetworkOptions();
            default:
                return [];
        }
    }

    private _getNetworkOptions() {
        if (!this.networks) return [];

        const options = [];
        for (const network of this.networks) {
            if (network.environments) {
                for (const environment of network.environments) {
                    options.push({
                        value: environment.id,
                        label: `${network.title} - ${environment.title}`
                    });
                }
            }
        }
        return options;
    }

    private _setupSubscriptions(): void {
        this._feedTableService.feeds$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((feeds: Feed[]) => {
                this.feeds = feeds;
            });

        this._feedTableService.totalCount$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((total: number) => {
                this.total = total;
            });

        this._feedTableService.networks$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((networks: Network[] | null) => {
                if (networks) {
                    this.networks = networks;
                }
            });

        this._feedTableService.tags$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((tags: Tag[]) => {
                if (tags) {
                    this.tags = tags;
                }
            });

        this._feedTableService.feedStatuses$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(statuses => {
                this.streamData = statuses;
            });

        this._filterService.filters$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((filters: FilterElement[]) => {
                if (this.tableState) {
                    this.tableState.filters = filters;
                }
            });

        this._orderService.sortState$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((sort: Sort | null) => {
                if (this.tableState) {
                    this.tableState.sort = sort;
                }
            });

        this._searchService.searchTerm$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((searchTerm: string) => {
                if (this.tableState) {
                    this.tableState.searchTerm = searchTerm;
                }
            });

        this._paginationService.paginationState$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((paginationState: PaginationState) => {
                if (this.tableState) {
                    this.tableState.pagination = paginationState;
                }
            });
    }

    private _setupReactiveUpdates(): void {
        const searchAndFilters$ = combineLatest([
            this._searchService.searchTerm$.pipe(
                distinctUntilChanged()
            ),
            this._filterService.filters$,
        ]);

        const paginationAndSort$ = combineLatest([
            this._paginationService.paginationState$,
            this._orderService.sortState$
        ]);

        combineLatest([
            searchAndFilters$,
            paginationAndSort$
        ])
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe(([ 
            [searchTerm, filters],
            [paginationState, sortState]
        ]) => {
            const isSearchChange = this._searchService
                .isSearchChanged(searchTerm);

            const isFilterChange = this._filterService
                .isFiltersChanged(filters);

            const isSearchOrFilterChange = isSearchChange || isFilterChange;

            let saveStateAndLoadFeed = () => {
                this._tableStateService.saveTableState(
                    FEED_TABLE_PAGE_KEY,
                    this.tableState);

                this._loadFeeds();
            }

            isSearchOrFilterChange
                ? this._paginationService.resetToFirstPage()
                : saveStateAndLoadFeed();
        });
    }

    private _applyTableState(tableState: TableState): void {
        this.tableState = tableState;

        this._filterService.setFilters(tableState.filters);
        this._searchService.setSearchTerm(tableState.searchTerm);
        this._orderService.setSort(tableState.sort);

        this._paginationService.setPagination(
            tableState.pagination.pageIndex,
            tableState.pagination.pageSize
        );

        this._searchService.setSearchFields(
            FEED_TABLE_CONFIG.getSearchFields());
    }

    private _loadFeeds(silent: boolean = false): void {
        if (!silent) {
            this.isLoading = true;
        } else {
            this._fuseLoadingService.setAutoMode(false);
        }

        let queryParams = this._odataService.buildQuery(
            this.isLargeScreen
                ? this._filterService.currentFilters
                : [],
            this._searchService.currentSearchTerm,
            this._searchService.searchFields,
            this.isLargeScreen
                ? this._orderService.currentSort
                : null,
            this._paginationService.currentPagination
        );

        this._feedTableService.getFeeds(
            queryParams.search,
            queryParams.orderby,
            queryParams.filter,
            queryParams.skip,
            queryParams.top
        ).pipe(
            finalize(() => {
                if (silent) {
                    this._fuseLoadingService.setAutoMode(true);
                }
            })
        ).subscribe({
            next: () => {
                if (silent) {
                    return;
                }

                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;
                    });
            },
            error: (error) => {
                if (silent) {
                    return;
                }

                this.isLoading = false;

                this._notificationService.showErrorAlert(
                    'Error Loading Feeds',
                    error.title || 'Failed to load feeds'
                );
            }
        });
    }

    private _setupPolling(): void {
        const POLL_INTERVAL_MS = 5000;

        interval(POLL_INTERVAL_MS)
            .pipe(
                filter(() => document.visibilityState === 'visible' && !this.isLoading),
                takeUntil(this._unsubscribeAll)
            )
            .subscribe(() => this._loadFeeds(true));
    }

    private _performDelete(feed: Feed): void {
        this.isLoading = true;

        this._feedTableService.deleteFeed(feed.id!).subscribe({
            next: () => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;

                        this._notificationService.showSuccessAlert(
                            'Feed Deleted',
                            `"${feed.name}" has been successfully deleted.`
                        );
                    });
            },
            error: (error) => {
                this.isLoading = false;

                this._notificationService.showErrorAlert(
                    'Delete Failed',
                    error.title || 'Failed to delete feed'
                );
            }
        });
    }
}