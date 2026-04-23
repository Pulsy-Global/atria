import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { FilterElement, FilterModalResult } from '../../modals/filter/filter-modal.types';

@Injectable()
export class TableFilterService {
    private _filters = new BehaviorSubject<FilterElement[]>([]);

    get filters$(): Observable<FilterElement[]> {
        return this._filters.asObservable();
    }

    get currentFilters(): FilterElement[] {
        return this._filters.value;
    }

    getCurrentFilter(field: string): FilterElement | null {
        return this.currentFilters.find(f => f.field === field) || null;
    }

    addFilterFromModal(modalResult: FilterModalResult): void {
        const newFilter = FilterElement.fromModalResult(modalResult);
        const filters = [...this.currentFilters];
        
        const existingIndex = filters.findIndex(
            f => f.field === modalResult.columnConfig.key);

        if (existingIndex !== -1) {
            filters[existingIndex] = newFilter;
        } else {
            filters.push(newFilter);
        }

        this._filters.next(filters);
    }

    setFilters(filters: FilterElement[]) : void {
        this._filters.next(filters);
    }

    removeFilter(field: string): void {
        const filters = this.currentFilters
            .filter(f => f.field !== field);

        this._filters.next(filters);
    }

    clearAllFilters(): void {
        this._filters.next([]);
    }

    isFiltersChanged(newFilters: FilterElement[]): boolean {
        const currentFilters = this.currentFilters;

        return JSON.stringify(newFilters) !== 
               JSON.stringify(currentFilters);
    }
}