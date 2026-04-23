import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Sort } from '@angular/material/sort';

@Injectable()
export class TableOrderService {
    private _sortState = new BehaviorSubject<Sort | null>(null);

    get sortState$(): Observable<Sort | null> {
        return this._sortState.asObservable();
    }

    get currentSort(): Sort | null {
        return this._sortState.value;
    }

    setSort(sort: Sort): void {
        this._sortState.next(sort);
    }

    clearSort(): void {
        this._sortState.next(null);
    }

    toggleSort(field: string): void {
        const currentSort = this.currentSort;
        
        if (!currentSort || currentSort.active !== field) {
            this.setSort({ active: field, direction: 'asc' });
        } else if (currentSort.direction === 'asc') {
            this.setSort({ active: field, direction: 'desc' });
        } else {
            this.clearSort();
        }
    }
}