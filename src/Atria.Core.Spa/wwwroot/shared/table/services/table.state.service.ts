import { Injectable } from '@angular/core';
import { TableState } from '../table.types';
import { FilterElement } from '../../modals/filter/filter-modal.types';
import { STRING_EMPTY } from '../../core/constants/common.constants';

@Injectable({
    providedIn: 'root'
})
export class TableStateService {
    
    private readonly STORAGE_PREFIX = 'table_state_';

    getTableState(pageKey: string): TableState {
        const defaultState = this._getDefaultState();

        try {
            const stored = localStorage.getItem(this.STORAGE_PREFIX + pageKey);

            if (stored) {
                return this._validateAndNormalizeState(JSON.parse(stored));
            } else {
                this.saveTableState(pageKey, defaultState);
            }
        } catch (error) {
            this.saveTableState(pageKey, defaultState);
        }

        return defaultState;
    }

    saveTableState(pageKey: string, state: TableState): void {
        localStorage.setItem(this.STORAGE_PREFIX + pageKey, JSON.stringify(state));
    }

    clearTableState(pageKey: string): void {
        localStorage.removeItem(this.STORAGE_PREFIX + pageKey);
    }

    private _getDefaultState(): TableState {
        return {
            sort: null,
            filters: [],
            searchTerm: STRING_EMPTY,
            pagination: {
                pageIndex: 0,
                pageSize: 10
            }
        };
    }

    private _validateAndNormalizeState(state: any): TableState {
        return {
            sort: this._validateSort(state.sort),
            filters: this._validateFilters(state.filters),
            searchTerm: state.searchTerm,
            pagination: {
                pageIndex: state.pagination.pageIndex,
                pageSize: state.pagination.pageSize
            }
        };
    }

    private _validateSort(sort: any): TableState['sort'] {
        if (!sort || typeof sort !== 'object') {
            return null;
        }

        return {
            active: sort.active,
            direction: sort.direction
        };
    }

    private _validateFilters(filters: any): FilterElement[] {
        if (!Array.isArray(filters)) {
            return [];
        }

        return filters.map(filter => new FilterElement(
            filter.field,
            filter.type,
            filter.value,
            filter.displayText
        ));
    }
}