import { Injectable } from '@angular/core';
import { Observable, throwError, BehaviorSubject, of } from 'rxjs';
import { catchError, tap, switchMap } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { Output, Tag, CreateTag, ProblemDetails } from '../../api/api.client';
import { OutputRequest } from './output.types';
import { OutputOperation } from './output.types';

@Injectable({
    providedIn: 'root'
})
export class OutputService {
    
    private _output: BehaviorSubject<Output | null> = new BehaviorSubject<Output | null>(null);
    private _tags: BehaviorSubject<Tag[]> = new BehaviorSubject<Tag[]>([]);

    get output$(): Observable<Output | null> {
        return this._output.asObservable();
    }

    get tags$(): Observable<Tag[]> {
        return this._tags.asObservable();
    }

    constructor(private apiService: ApiService) {}

    clearState(): void {
        this._output.next(null);
    }

    getOutput(outputId: string): Observable<Output> {
        return this.apiService.apiClient.getOutput(outputId).pipe(
            tap((output: Output) => {
                this._output.next(output);
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

    createTag(name: string, color: string): Observable<Tag> {
        const createTagDto = new CreateTag({ 
            name: name, 
            color: color, 
            type: 'Output' 
        });
        
        return this.apiService.apiClient.createTag(createTagDto).pipe(
            tap((tag: Tag) => {
                const currentTags = this._tags.value;
                this._tags.next([...currentTags, tag]);
            }),
            catchError((error): Observable<any> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    saveOutput(request: OutputRequest): Observable<Output> {
        const operation = request.operation === OutputOperation.Create
            ? this.apiService.apiClient.createOutput(request.outputData)
            : this.apiService.apiClient.updateOutput(request.outputId!, request.outputData);

        return operation.pipe(
            switchMap((output: Output) => {
                return of(output);
            }),
            catchError((error) => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }
}