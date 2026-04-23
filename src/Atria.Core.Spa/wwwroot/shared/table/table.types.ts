import { FilterType } from './odata.types';
import { FilterElement } from '../modals/filter/filter-modal.types';
import { Sort } from '@angular/material/sort';

export interface ColumnConfig {
    key: string;
    label: string;
    sortable: boolean;
    filterType: FilterType;
}

export interface PaginationState {
    pageIndex: number;
    pageSize: number;
}

export interface TableState {
    sort: Sort | null;
    filters: FilterElement[];
    searchTerm: string;
    pagination: PaginationState;
}