import { Injectable } from '@angular/core';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { Output, PagedList_OutputDto, ProblemDetails, Tag } from '../../api/api.client';

@Injectable({
    providedIn: 'root'
})
export class OutputTableService {
    
    private _outputs = new BehaviorSubject<Output[]>([]);
    private _totalCount = new BehaviorSubject<number>(0);
    private _tags = new BehaviorSubject<Tag[]>([]);

    get outputs$(): Observable<Output[]> {
        return this._outputs.asObservable();
    }

    get totalCount$(): Observable<number> {
        return this._totalCount.asObservable();
    }

    get tags$(): Observable<Tag[]> {
        return this._tags.asObservable();
    }

    constructor(
        private apiService: ApiService
    ) {}

    clearState(): void {
        this._outputs.next([]);
        this._totalCount.next(0);
        this._tags.next([]);
    }

    getOutputs(
        search?: string,
        orderby?: string,
        filter?: string,
        skip?: number,
        top?: number
    ): Observable<PagedList_OutputDto> {
        return this.apiService.apiClient.getOutputs(
            orderby,
            filter,
            skip,
            top,
            search
        ).pipe(
            tap((pagedResult: PagedList_OutputDto) => {
                this._outputs.next(pagedResult.items || []);
                this._totalCount.next(pagedResult.totalCount || 0);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getTags(): Observable<Tag[]> {
        return this.apiService.apiClient.getOutputTags().pipe(
            tap((tags: Tag[]) => {
                this._tags.next(tags);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    deleteOutput(outputId: string): Observable<void> {
        return this.apiService.apiClient.deleteOutput(outputId).pipe(
            tap(() => {
                const currentOutputs = this._outputs.value;
                const currentTotal = this._totalCount.value;
                
                const updatedOutputs = currentOutputs.filter(output => output.id !== outputId);
                
                this._outputs.next(updatedOutputs);
                this._totalCount.next(Math.max(0, currentTotal - 1));
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }
}