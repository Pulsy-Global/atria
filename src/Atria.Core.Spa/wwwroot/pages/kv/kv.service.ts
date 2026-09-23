import { Injectable, inject } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { BucketList, BucketValues, ProblemDetails } from '../../api/api.client';

@Injectable({
    providedIn: 'root'
})
export class KvService {

    private readonly apiService = inject(ApiService);

    listBuckets(limit?: number, cursor?: string): Observable<BucketList> {
        return this.apiService.apiClient.getBuckets(limit, cursor).pipe(
            catchError((error): Observable<never> => {
                return throwError(() => new ProblemDetails(error));
            })
        );
    }

    getBucketValues(
        bucket: string,
        limit: number,
        cursor?: string,
        prefix?: string
    ): Observable<BucketValues> {
        return this.apiService.apiClient
            .getBucketValues(bucket, limit, cursor, prefix)
            .pipe(
                catchError((error): Observable<never> => {
                    return throwError(() => new ProblemDetails(error));
                })
            );
    }

    getBucketValue(bucket: string, key: string): Observable<string | undefined> {
        return this.apiService.apiClient.getBucket(bucket, key).pipe(
            map((value) => value.value),
            catchError((error): Observable<string | undefined> => {
                if (error instanceof ProblemDetails && error.status === 404) {
                    return of(undefined);
                }

                return throwError(() => new ProblemDetails(error));
            })
        );
    }
}
