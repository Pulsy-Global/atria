import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { PaginationState } from '../table.types';

@Injectable()
export class TablePaginationService {
    private _paginationState = new BehaviorSubject<PaginationState>({
        pageIndex: 0,
        pageSize: 10,
    });

    get paginationState$(): Observable<PaginationState> {
        return this._paginationState.asObservable();
    }

    get currentPagination(): PaginationState {
        return this._paginationState.value;
    }
    
    setPagination(pageIndex?: number, pageSize?: number): void {
        const current = this.currentPagination;

        if (current.pageIndex !== pageIndex || 
            current.pageSize !== pageSize) {
            this._paginationState.next({
                ...current,
                pageSize,
                pageIndex
            });
        }
    }

    resetToFirstPage(): void {
        const current = this.currentPagination;
        
        this._paginationState.next({
            ...current,
            pageIndex: 0
        });
    }
}