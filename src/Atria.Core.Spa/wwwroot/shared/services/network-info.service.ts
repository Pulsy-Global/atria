import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, tap } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import { Network, Networks, ProblemDetails } from '../../api/api.client';

@Injectable({
    providedIn: 'root'
})
export class NetworkInfoService {

    private apiService = inject(ApiService);
    private _networks = new BehaviorSubject<Network[] | null>(null);
    private _loadNetworks$: Observable<Network[]> | null = null;

    get networks$(): Observable<Network[] | null> {
        return this._networks.asObservable();
    }

    getNetworks(): Observable<Network[]> {
        const networks = this._networks.value;

        if (networks) {
            return of(networks);
        }

        if (!this._loadNetworks$) {
            this._loadNetworks$ = this.apiService.apiClient.getNetworks().pipe(
                map((response: Networks) => response.networks || []),
                tap((loadedNetworks: Network[]) => {
                    this._networks.next(loadedNetworks);
                }),
                catchError((error): Observable<Network[]> => {
                    return throwError(() => new ProblemDetails(error));
                }),
                finalize(() => {
                    this._loadNetworks$ = null;
                }),
                shareReplay(1)
            );
        }

        return this._loadNetworks$;
    }
}
