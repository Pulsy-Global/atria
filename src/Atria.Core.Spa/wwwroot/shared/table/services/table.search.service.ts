import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { STRING_EMPTY } from '../../core/constants/common.constants';

@Injectable()
export class TableSearchService {
    private _searchTerm = new BehaviorSubject<string>(STRING_EMPTY);
    private _searchFields: string[] = [];

    get searchTerm$(): Observable<string> {
        return this._searchTerm.asObservable();
    }

    get currentSearchTerm(): string {
        return this._searchTerm.value;
    }

    get searchFields(): string[] {
        return [...this._searchFields];
    }

    setSearchTerm(searchTerm: string): void {
        this._searchTerm.next(searchTerm);
    }

    setSearchFields(searchFields: string[]): void {
        this._searchFields = [...searchFields];
    }

    isSearchChanged(newSearchTerm: string): boolean {
        return newSearchTerm !== this.currentSearchTerm;
    }

    reset(): void {
        this._searchTerm.next(STRING_EMPTY);
    }
}