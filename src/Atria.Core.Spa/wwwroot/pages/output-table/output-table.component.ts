import { Component, OnInit, OnDestroy, ViewEncapsulation, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { FuseCardComponent } from 'fuse/components/card';
import { OutputTableService } from './output-table.service';
import { NotificationService } from '../../shared/services/notification/notification.service';
import { ConfirmModalComponent, ConfirmModalData } from '../../shared/modals/confirm/confirm-modal.component';
import { FilterModalComponent } from '../../shared/modals/filter/filter-modal.component';
import { Subject, takeUntil, debounceTime, distinctUntilChanged, combineLatest, timer } from 'rxjs';
import { Output, OutputType, Tag } from '../../api/api.client';
import { ColumnConfig, TableState, PaginationState } from '../../shared/table/table.types';
import { SearchBarComponent } from '../../shared/core/search/search.component'
import { AtriaPaginationDirective } from '../../shared/core/paginator/paginator.directive'
import { FilterModalData, FilterModalResult, FilterElement } from '../../shared/modals/filter/filter-modal.types';
import { FilterType } from '../../shared/table/odata.types';
import { OUTPUT_TABLE_PAGE_KEY, STRING_EMPTY } from '../../shared/core/constants/common.constants';
import { TableStateService } from '../../shared/table/services/table.state.service';
import { TableFilterService } from '../../shared/table/services/table.filter.service';
import { TableSearchService } from '../../shared/table/services/table.search.service';
import { TableOrderService } from '../../shared/table/services/table.order.service';
import { TablePaginationService } from '../../shared/table/services/table.pagination.service';
import { TableODataService } from '../../shared/table/services/table.odata.service';
import { OUTPUT_TABLE_CONFIG } from './output-table.config';
import { OUTPUT_TYPE_CONFIG } from '../../shared/output/output-config.config';

@Component({
    selector: 'output-table',
    standalone: true,
    templateUrl: './output-table.component.html',
    styleUrls: ['./output-table.component.scss'],
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
        MatProgressSpinnerModule,
        AtriaPaginationDirective,
        MatTooltipModule,
        MatPaginatorModule,
        MatChipsModule,
        SearchBarComponent,
        FuseCardComponent
    ]
})
export class OutputTableComponent implements OnInit, OnDestroy {

    private _unsubscribeAll: Subject<any> = new Subject<any>();

    isLoading = true;
    isLargeScreen: boolean = true;

    outputs: Output[] = [];
    total: number = 0;
    tags: Tag[] = [];

    displayedColumns: string[] = [];
    tableState: TableState;

    readonly OutputType = OutputType;
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
        private readonly _outputTableService: OutputTableService,
        private readonly _notificationService: NotificationService,
        private readonly _tableStateService: TableStateService,
        private readonly _filterService: TableFilterService,
        private readonly _searchService: TableSearchService,
        private readonly _odataService: TableODataService,
        private readonly _orderService: TableOrderService,
        private readonly _paginationService: TablePaginationService,
        private readonly _dialog: MatDialog,
        private readonly _router: Router,
    ) {
        this.displayedColumns = OUTPUT_TABLE_CONFIG
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
        this._loadOutputs();
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
        this._outputTableService.clearState();
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
        
        const config = OUTPUT_TABLE_CONFIG.getField(column);
        
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

        dialogRef.afterClosed().subscribe((result: FilterModalResult | null) => {
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

    onCreateOutput(): void {
        this._router.navigate(['/output']);
    }

    onEditOutput(output: Output): void {
        this._router.navigate(['/output', output.id]);
    }

    onDeleteOutput(output: Output): void {
        const dialogData: ConfirmModalData = {
            title: 'Delete Output',
            message: `Are you sure you want to delete "${output.name}"? This action cannot be undone.`,
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
                this._performDelete(output);
            }
        });
    }

    isSortable(field: string): boolean {
        return OUTPUT_TABLE_CONFIG.getField(field)?.sortable || false;
    }

    isFiltrable(field: string): boolean {
        return OUTPUT_TABLE_CONFIG.getField(field)?.filterType !== FilterType.None;
    }

    isActiveFilter(field: string): boolean {
        return this.tableState?.filters?.some(f => f.field === field) || false;
    }

    getTypeColor(type: OutputType): string {
        return OUTPUT_TYPE_CONFIG.getTypeColor(type);
    }

    getTypeText(type: OutputType): string {
        return OUTPUT_TYPE_CONFIG.getTypeLabel(type);
    }

    getTypeIcon(type: OutputType): string {
        return OUTPUT_TYPE_CONFIG.getTypeIcon(type);
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
            case 'type':
                return OUTPUT_TYPE_CONFIG.getTypeLabel(value);
            case 'createdAt':
                return value ? new Date(value).toLocaleDateString() : STRING_EMPTY;
            default:
                return String(value || STRING_EMPTY);
        }
    }

    private _checkScreenSize(): void {
        this.isLargeScreen = window.innerWidth >= 1280;
    }

    private _getEnumOptions(field: string) {
        switch (field) {
            case 'type':
                return OUTPUT_TYPE_CONFIG.getTypes();
            default:
                return [];
        }
    }

    private _setupSubscriptions(): void {
        this._outputTableService.outputs$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((outputs: Output[]) => {
                this.outputs = outputs;
            });

        this._outputTableService.totalCount$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((total: number) => {
                this.total = total;
            });

        this._outputTableService.tags$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((tags: Tag[]) => {
                if (tags) {
                    this.tags = tags;
                }
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

            let saveStateAndLoadOutput = () => {
                this._tableStateService.saveTableState(
                    OUTPUT_TABLE_PAGE_KEY, 
                    this.tableState);
    
                this._loadOutputs();
            }

            isSearchOrFilterChange 
                ? this._paginationService.resetToFirstPage() 
                : saveStateAndLoadOutput();
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
            OUTPUT_TABLE_CONFIG.getSearchFields());
    }

    private _loadOutputs(): void {
        this.isLoading = true;
        
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

        this._outputTableService.getOutputs(
            queryParams.search,
            queryParams.orderby,
            queryParams.filter,
            queryParams.skip,
            queryParams.top
        ).subscribe({
            next: () => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;
                    });
            },
            error: (error) => {
                this.isLoading = false;

                this._notificationService.showErrorAlert(
                    'Error Loading Outputs',
                    error.title || 'Failed to load outputs'
                );
            }
        });
    }

    private _performDelete(output: Output): void {
        this.isLoading = true;

        this._outputTableService.deleteOutput(output.id!).subscribe({
            next: () => {
                timer(300)
                    .pipe(takeUntil(this._unsubscribeAll))
                    .subscribe(() => {
                        this.isLoading = false;

                        this._notificationService.showSuccessAlert(
                            'Output Deleted',
                            `"${output.name}" has been successfully deleted.` 
                        );
                    });
            },
            error: (error) => {
                this.isLoading = false;

                this._notificationService.showErrorAlert(
                    'Delete Failed',
                    error.title || 'Failed to delete output'
                );
            }
        });
    }
}